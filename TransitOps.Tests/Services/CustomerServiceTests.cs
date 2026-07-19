using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Features.Customers;
using TransitOps.Api.Persistence;

namespace TransitOps.Tests.Services;

public sealed class CustomerServiceTests
{
    [Fact]
    public async Task Crud_trims_optional_values_and_soft_deletes_without_imposing_name_uniqueness()
    {
        await using var db = CreateDatabase();
        var service = new CustomerService(db);
        var first = await service.CreateAsync(new(" Acme ", " info@acme.test "), default);
        var second = await service.CreateAsync(new("Acme", "   "), default);
        var updated = await service.UpdateAsync(first.Id, new("Acme Norte", null), default);
        await service.DeactivateAsync(first.Id, default);

        Assert.Equal("Acme Norte", updated.Name);
        Assert.Null(second.ContactDetails);
        Assert.Single(await service.GetAllAsync(default));
        Assert.Equal(2, await db.Customers.CountAsync());
        Assert.False((await db.Customers.FindAsync(first.Id))!.IsActive);
    }

    [Fact]
    public async Task Missing_customer_returns_not_found()
    {
        await using var db = CreateDatabase();
        var exception = await Assert.ThrowsAsync<ApiException>(() => new CustomerService(db).DeactivateAsync(Guid.NewGuid(), default));
        Assert.Equal(404, exception.StatusCode);
        Assert.Equal("customer_not_found", exception.Code);
    }

    private static TransitOpsDbContext CreateDatabase() => new(new DbContextOptionsBuilder<TransitOpsDbContext>()
        .UseInMemoryDatabase($"customer-tests-{Guid.NewGuid():N}").Options);
}
