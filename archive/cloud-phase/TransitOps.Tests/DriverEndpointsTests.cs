using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Infrastructure.Persistence;

namespace TransitOps.Tests;

public sealed class DriverEndpointsTests
{
    [Fact]
    public async Task Create_ReturnsCreatedAndPersistsDriver_WhenRequestIsValid()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/drivers",
            CreateDriverRequest(licenseNumber: "LIC-200", email: "driver200@transitops.dev"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var payload = await ReadJsonAsync(response);
        var driverId = payload["data"]?["id"]?.GetValue<string>();

        Assert.NotNull(driverId);
        Assert.Equal("LIC-200", payload["data"]?["licenseNumber"]?.GetValue<string>());
        Assert.Equal("driver200@transitops.dev", payload["data"]?["email"]?.GetValue<string>());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var driver = await dbContext.Drivers.SingleAsync();

        Assert.Equal(Guid.Parse(driverId!), driver.Id);
        Assert.Equal("LIC-200", driver.LicenseNumber);
        Assert.Equal(DateTimeKind.Unspecified, driver.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Unspecified, driver.UpdatedAt.Kind);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenLicenseNumberAlreadyExistsOnActiveDriver()
    {
        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Drivers.Add(CreateDriver("LIC-201"));
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/drivers",
            CreateDriverRequest(licenseNumber: "LIC-201", email: "new@transitops.dev"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("driver_license_number_conflict", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenEmailAlreadyExistsOnActiveDriver()
    {
        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Drivers.Add(CreateDriver("LIC-202", email: "driver202@transitops.dev"));
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/drivers",
            CreateDriverRequest(
                licenseNumber: "LIC-203",
                employeeCode: "DRV-203",
                email: "driver202@transitops.dev"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("driver_email_conflict", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenEmailIsInvalid()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/drivers",
            CreateDriverRequest(licenseNumber: "LIC-204", email: "not-an-email"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("validation_error", payload["error"]?["code"]?.GetValue<string>());
        Assert.NotNull(payload["error"]?["details"]?["Email"]);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyActiveDrivers_OrderedByLastNameThenFirstName()
    {
        var firstDriver = CreateDriver("LIC-205", firstName: "Ana", lastName: "Alonso");
        var secondDriver = CreateDriver("LIC-206", firstName: "Bea", lastName: "Bravo");
        var deletedDriver = CreateDriver(
            "LIC-207",
            firstName: "Zoey",
            lastName: "Zuluaga",
            deletedAt: new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc));

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Drivers.AddRange(firstDriver, secondDriver, deletedDriver);
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/drivers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var drivers = payload["data"]!.AsArray();

        Assert.Equal(2, drivers.Count);
        Assert.Equal(firstDriver.LastName, drivers[0]?["lastName"]?.GetValue<string>());
        Assert.Equal(secondDriver.LastName, drivers[1]?["lastName"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetById_ReturnsDriver_WhenDriverExists()
    {
        var driver = CreateDriver("LIC-208", email: "driver208@transitops.dev");

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Drivers.Add(driver);
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/drivers/{driver.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal(driver.Id.ToString(), payload["data"]?["id"]?.GetValue<string>());
        Assert.Equal(driver.LicenseNumber, payload["data"]?["licenseNumber"]?.GetValue<string>());
        Assert.Equal(driver.Email, payload["data"]?["email"]?.GetValue<string>());
    }

    [Fact]
    public async Task Update_ReturnsUpdatedDriver_WhenDriverExists()
    {
        var driver = CreateDriver("LIC-209", email: "driver209@transitops.dev");

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Drivers.Add(driver);
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/drivers/{driver.Id}",
            CreateDriverRequest(
                licenseNumber: "LIC-209-B",
                firstName: "Lucia",
                lastName: "Mena",
                employeeCode: "DRV-209-B",
                email: "driver209b@transitops.dev",
                phone: "+34911111222",
                isActive: false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("LIC-209-B", payload["data"]?["licenseNumber"]?.GetValue<string>());
        Assert.False(payload["data"]?["isActive"]?.GetValue<bool>());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var storedDriver = await dbContext.Drivers.SingleAsync();

        Assert.Equal("LIC-209-B", storedDriver.LicenseNumber);
        Assert.Equal("Lucia", storedDriver.FirstName);
        Assert.Equal("Mena", storedDriver.LastName);
        Assert.False(storedDriver.IsActive);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_AndSoftDeletesDriver_WhenDriverExists()
    {
        var driver = CreateDriver("LIC-210");

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Drivers.Add(driver);
        });
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/drivers/{driver.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var storedDriver = await dbContext.Drivers.SingleAsync();

        Assert.NotNull(storedDriver.DeletedAt);
        Assert.Equal(storedDriver.DeletedAt, storedDriver.UpdatedAt);
    }

    [Fact]
    public async Task Create_AllowsReusingLicenseNumber_WhenPreviousDriverWasSoftDeleted()
    {
        var deletedDriver = CreateDriver(
            "LIC-211",
            deletedAt: new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc));

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Drivers.Add(deletedDriver);
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/drivers",
            CreateDriverRequest(licenseNumber: "LIC-211", email: "driver211@transitops.dev"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var drivers = await dbContext.Drivers
            .OrderBy(driver => driver.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, drivers.Count);
        Assert.Equal(1, drivers.Count(driver => driver.LicenseNumber == "LIC-211" && driver.DeletedAt is null));
        Assert.Equal(1, drivers.Count(driver => driver.LicenseNumber == "LIC-211" && driver.DeletedAt is not null));
    }

    private static Driver CreateDriver(
        string licenseNumber,
        string firstName = "Laura",
        string lastName = "Santos",
        string? employeeCode = "DRV-001",
        string? email = "driver@transitops.dev",
        DateTime? deletedAt = null)
    {
        return new Driver
        {
            EmployeeCode = employeeCode,
            FirstName = firstName,
            LastName = lastName,
            LicenseNumber = licenseNumber,
            LicenseExpiryDate = new DateOnly(2027, 12, 31),
            Phone = "+34911111222",
            Email = email,
            IsActive = true,
            CreatedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
            DeletedAt = deletedAt
        };
    }

    private static object CreateDriverRequest(
        string licenseNumber,
        string firstName = "Laura",
        string lastName = "Santos",
        string? employeeCode = "DRV-001",
        string? phone = "+34911111222",
        string? email = "driver@transitops.dev",
        bool isActive = true)
    {
        return new
        {
            employeeCode,
            firstName,
            lastName,
            licenseNumber,
            licenseExpiryDate = new DateOnly(2027, 12, 31),
            phone,
            email,
            isActive
        };
    }

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!;
    }
}
