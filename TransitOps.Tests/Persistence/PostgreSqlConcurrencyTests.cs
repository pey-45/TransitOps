using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Features.Auth;
using TransitOps.Api.Features.Users;
using TransitOps.Api.Persistence;

namespace TransitOps.Tests.Persistence;

public sealed class PostgreSqlConcurrencyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var db = CreateDatabase();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task PostgreSql_guarantees_assignment_and_last_admin_rules_under_concurrency()
    {
        await VerifyExclusiveAssignmentAsync(assignVehicle: true);
        await VerifyExclusiveAssignmentAsync(assignVehicle: false);
        await VerifyLastAdminProtectionAsync();
    }

    private async Task VerifyExclusiveAssignmentAsync(bool assignVehicle)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var firstShipment = Shipment($"CONCURRENCY-A-{suffix}");
        var secondShipment = Shipment($"CONCURRENCY-B-{suffix}");
        var firstVehicle = new Vehicle { LicensePlate = $"V-A-{suffix}" };
        var secondVehicle = new Vehicle { LicensePlate = $"V-B-{suffix}" };
        var firstDriver = new Driver { Name = "Driver A", LicenseNumber = $"D-A-{suffix}" };
        var secondDriver = new Driver { Name = "Driver B", LicenseNumber = $"D-B-{suffix}" };

        await using (var seed = CreateDatabase())
        {
            seed.AddRange(firstShipment, secondShipment, firstVehicle, secondVehicle, firstDriver, secondDriver);
            await seed.SaveChangesAsync();
        }

        await using var first = CreateDatabase();
        await using var second = CreateDatabase();
        var firstTracked = await first.Shipments.SingleAsync(item => item.Id == firstShipment.Id);
        var secondTracked = await second.Shipments.SingleAsync(item => item.Id == secondShipment.Id);

        firstTracked.VehicleId = firstVehicle.Id;
        firstTracked.DriverId = firstDriver.Id;
        secondTracked.VehicleId = assignVehicle ? firstVehicle.Id : secondVehicle.Id;
        secondTracked.DriverId = assignVehicle ? secondDriver.Id : firstDriver.Id;

        var results = await Task.WhenAll(CaptureSaveAsync(first), CaptureSaveAsync(second));
        Assert.Single(results, result => result is null);
        var failure = Assert.Single(results, result => result is not null)!;
        var postgres = Assert.IsType<PostgresException>(failure.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal(
            assignVehicle
                ? TransitOpsDbContext.OpenShipmentVehicleIndex
                : TransitOpsDbContext.OpenShipmentDriverIndex,
            postgres.ConstraintName);

        await using var verification = CreateDatabase();
        var assignments = assignVehicle
            ? await verification.Shipments.CountAsync(item =>
                item.VehicleId == firstVehicle.Id &&
                (item.Status == ShipmentStatus.Planned || item.Status == ShipmentStatus.InProgress))
            : await verification.Shipments.CountAsync(item =>
                item.DriverId == firstDriver.Id &&
                (item.Status == ShipmentStatus.Planned || item.Status == ShipmentStatus.InProgress));
        Assert.Equal(1, assignments);
    }

    private async Task VerifyLastAdminProtectionAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var firstAdmin = User($"admin-a-{suffix}");
        var secondAdmin = User($"admin-b-{suffix}");
        await using (var seed = CreateDatabase())
        {
            seed.AppUsers.AddRange(firstAdmin, secondAdmin);
            await seed.SaveChangesAsync();
        }

        await using var first = CreateDatabase();
        await using var second = CreateDatabase();
        var firstService = new UserService(first, new PasswordHasher<AppUser>());
        var secondService = new UserService(second, new PasswordHasher<AppUser>());

        var results = await Task.WhenAll(
            CaptureApiExceptionAsync(() => firstService.ChangeActivationAsync(firstAdmin.Id, new(false), default)),
            CaptureApiExceptionAsync(() => secondService.ChangeActivationAsync(secondAdmin.Id, new(false), default)));

        Assert.Single(results, result => result is null);
        var failure = Assert.Single(results, result => result is not null)!;
        Assert.Equal(409, failure.StatusCode);
        Assert.Equal("last_admin_protected", failure.Code);

        await using var verification = CreateDatabase();
        Assert.Equal(1, await verification.AppUsers.CountAsync(item =>
            item.IsActive && item.Role == UserRole.Admin &&
            (item.Id == firstAdmin.Id || item.Id == secondAdmin.Id)));
    }

    private static async Task<DbUpdateException?> CaptureSaveAsync(TransitOpsDbContext dbContext)
    {
        try
        {
            await dbContext.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateException exception)
        {
            return exception;
        }
    }

    private static async Task<ApiException?> CaptureApiExceptionAsync(Func<Task<UserResponse>> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (ApiException exception)
        {
            return exception;
        }
    }

    private TransitOpsDbContext CreateDatabase() => new(
        new DbContextOptionsBuilder<TransitOpsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options);

    private static Shipment Shipment(string reference) => new()
    {
        Reference = reference,
        Origin = "Madrid",
        Destination = "Barcelona",
        PlannedPickupAt = DateTime.UtcNow.AddHours(1)
    };

    private static AppUser User(string username) => new()
    {
        Username = username,
        Email = $"{username}@transitops.test",
        PasswordHash = "hash",
        Role = UserRole.Admin
    };
}
