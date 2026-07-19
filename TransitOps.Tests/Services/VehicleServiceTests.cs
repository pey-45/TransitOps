using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Features.Vehicles;
using TransitOps.Api.Persistence;

namespace TransitOps.Tests.Services;

public sealed class VehicleServiceTests
{
    [Fact]
    public async Task Create_normalizes_fields_and_rejects_active_business_identifier_conflicts()
    {
        await using var db = CreateDatabase();
        var service = new VehicleService(db);
        var created = await service.CreateAsync(new(" 1234 abc ", " F-01 ", " Volvo ", " FH ", 12000), default);

        Assert.Equal("1234 ABC", created.LicensePlate);
        Assert.Equal("F-01", created.InternalCode);
        Assert.Equal("Volvo", created.Brand);
        var plateConflict = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(new("1234 ABC", null, null, null, null), default));
        var codeConflict = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(new("9999 XYZ", "F-01", null, null, null), default));
        Assert.Equal("vehicle_plate_conflict", plateConflict.Code);
        Assert.Equal("vehicle_internal_code_conflict", codeConflict.Code);
    }

    [Fact]
    public async Task Update_excludes_itself_and_deactivation_preserves_row_but_removes_it_from_daily_list()
    {
        await using var db = CreateDatabase();
        var service = new VehicleService(db);
        var created = await service.CreateAsync(new("1234 ABC", "F-01", null, null, null), default);
        var updated = await service.UpdateAsync(created.Id, new("1234 ABC", "F-01", "MAN", null, null), default);
        await service.DeactivateAsync(created.Id, default);

        Assert.Equal("MAN", updated.Brand);
        Assert.Empty(await service.GetAllAsync(default));
        Assert.False((await db.Vehicles.SingleAsync()).IsActive);
        var replacement = await service.CreateAsync(new("1234 ABC", "F-01", null, null, null), default);
        Assert.NotEqual(created.Id, replacement.Id);
    }

    [Fact]
    public async Task Updating_or_deactivating_an_inactive_or_missing_vehicle_returns_not_found()
    {
        await using var db = CreateDatabase();
        db.Vehicles.Add(new Vehicle { LicensePlate = "1234 ABC", IsActive = false });
        await db.SaveChangesAsync();
        var service = new VehicleService(db);
        var id = (await db.Vehicles.SingleAsync()).Id;

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.DeactivateAsync(id, default));
        Assert.Equal(404, exception.StatusCode);
        Assert.Equal("vehicle_not_found", exception.Code);
    }

    private static TransitOpsDbContext CreateDatabase() => new(new DbContextOptionsBuilder<TransitOpsDbContext>()
        .UseInMemoryDatabase($"vehicle-tests-{Guid.NewGuid():N}").Options);
}
