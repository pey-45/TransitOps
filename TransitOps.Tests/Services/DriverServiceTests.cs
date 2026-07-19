using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Features.Drivers;
using TransitOps.Api.Persistence;

namespace TransitOps.Tests.Services;

public sealed class DriverServiceTests
{
    [Fact]
    public async Task Crud_normalizes_enforces_active_license_uniqueness_and_soft_deletes()
    {
        await using var db = CreateDatabase();
        var service = new DriverService(db);
        var created = await service.CreateAsync(new(" Ana Pérez ", " b-123 ", " E-7 ", " ana@example.test "), default);

        Assert.Equal("Ana Pérez", created.Name);
        Assert.Equal("B-123", created.LicenseNumber);
        var conflict = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(new("Otra", "B-123", null, null), default));
        Assert.Equal("driver_license_conflict", conflict.Code);

        var updated = await service.UpdateAsync(created.Id, new("Ana P.", "B-123", null, null), default);
        Assert.Equal("Ana P.", updated.Name);
        await service.DeactivateAsync(created.Id, default);
        Assert.Empty(await service.GetAllAsync(default));
        Assert.False((await db.Drivers.SingleAsync()).IsActive);
        Assert.NotNull(await service.CreateAsync(new("Otra", "B-123", null, null), default));
    }

    [Fact]
    public async Task Missing_driver_returns_not_found()
    {
        await using var db = CreateDatabase();
        var exception = await Assert.ThrowsAsync<ApiException>(() => new DriverService(db).UpdateAsync(
            Guid.NewGuid(), new("Name", "License", null, null), default));
        Assert.Equal(404, exception.StatusCode);
        Assert.Equal("driver_not_found", exception.Code);
    }

    private static TransitOpsDbContext CreateDatabase() => new(new DbContextOptionsBuilder<TransitOpsDbContext>()
        .UseInMemoryDatabase($"driver-tests-{Guid.NewGuid():N}").Options);
}
