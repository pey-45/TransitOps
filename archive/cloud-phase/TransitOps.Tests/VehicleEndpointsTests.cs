using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Infrastructure.Persistence;

namespace TransitOps.Tests;

public sealed class VehicleEndpointsTests
{
    [Fact]
    public async Task Create_ReturnsCreatedAndPersistsVehicle_WhenRequestIsValid()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/vehicles",
            CreateVehicleRequest(plateNumber: "1234ABC", internalCode: "VEH-200"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var payload = await ReadJsonAsync(response);
        var vehicleId = payload["data"]?["id"]?.GetValue<string>();

        Assert.NotNull(vehicleId);
        Assert.Equal("1234ABC", payload["data"]?["plateNumber"]?.GetValue<string>());
        Assert.Equal("VEH-200", payload["data"]?["internalCode"]?.GetValue<string>());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var vehicle = await dbContext.Vehicles.SingleAsync();

        Assert.Equal(Guid.Parse(vehicleId!), vehicle.Id);
        Assert.Equal("1234ABC", vehicle.PlateNumber);
        Assert.Equal("VEH-200", vehicle.InternalCode);
        Assert.Equal(DateTimeKind.Unspecified, vehicle.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Unspecified, vehicle.UpdatedAt.Kind);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenPlateNumberAlreadyExistsOnActiveVehicle()
    {
        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Vehicles.Add(CreateVehicle("1234ABC"));
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/vehicles",
            CreateVehicleRequest(plateNumber: "1234ABC", internalCode: "VEH-201"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("vehicle_plate_number_conflict", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenInternalCodeAlreadyExistsOnActiveVehicle()
    {
        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Vehicles.Add(CreateVehicle("1234ABC", internalCode: "VEH-202"));
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/vehicles",
            CreateVehicleRequest(plateNumber: "5678DEF", internalCode: "VEH-202"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("vehicle_internal_code_conflict", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenCapacityIsNegative()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/vehicles",
            CreateVehicleRequest(plateNumber: "9999ZZZ", capacityKg: -1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("validation_error", payload["error"]?["code"]?.GetValue<string>());
        Assert.NotNull(payload["error"]?["details"]?["CapacityKg"]);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyActiveVehicles_OrderedByPlateNumber()
    {
        var firstVehicle = CreateVehicle("1111AAA");
        var secondVehicle = CreateVehicle("2222BBB");
        var deletedVehicle = CreateVehicle(
            "0000ZZZ",
            deletedAt: new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc));

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Vehicles.AddRange(firstVehicle, secondVehicle, deletedVehicle);
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/vehicles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var vehicles = payload["data"]!.AsArray();

        Assert.Equal(2, vehicles.Count);
        Assert.Equal(firstVehicle.PlateNumber, vehicles[0]?["plateNumber"]?.GetValue<string>());
        Assert.Equal(secondVehicle.PlateNumber, vehicles[1]?["plateNumber"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetById_ReturnsVehicle_WhenVehicleExists()
    {
        var vehicle = CreateVehicle("1234ABC", internalCode: "VEH-203");

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Vehicles.Add(vehicle);
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/vehicles/{vehicle.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal(vehicle.Id.ToString(), payload["data"]?["id"]?.GetValue<string>());
        Assert.Equal(vehicle.PlateNumber, payload["data"]?["plateNumber"]?.GetValue<string>());
        Assert.Equal(vehicle.InternalCode, payload["data"]?["internalCode"]?.GetValue<string>());
    }

    [Fact]
    public async Task Update_ReturnsUpdatedVehicle_WhenVehicleExists()
    {
        var vehicle = CreateVehicle("1234ABC", internalCode: "VEH-204");

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Vehicles.Add(vehicle);
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/vehicles/{vehicle.Id}",
            CreateVehicleRequest(
                plateNumber: "4321CBA",
                internalCode: "VEH-204-B",
                brand: "Mercedes",
                model: "Actros",
                capacityKg: 12000,
                capacityM3: 60,
                isActive: false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("4321CBA", payload["data"]?["plateNumber"]?.GetValue<string>());
        Assert.False(payload["data"]?["isActive"]?.GetValue<bool>());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var storedVehicle = await dbContext.Vehicles.SingleAsync();

        Assert.Equal("4321CBA", storedVehicle.PlateNumber);
        Assert.Equal("VEH-204-B", storedVehicle.InternalCode);
        Assert.Equal("Mercedes", storedVehicle.Brand);
        Assert.False(storedVehicle.IsActive);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_AndSoftDeletesVehicle_WhenVehicleExists()
    {
        var vehicle = CreateVehicle("1234ABC");

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Vehicles.Add(vehicle);
        });
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/vehicles/{vehicle.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var storedVehicle = await dbContext.Vehicles.SingleAsync();

        Assert.NotNull(storedVehicle.DeletedAt);
        Assert.Equal(storedVehicle.DeletedAt, storedVehicle.UpdatedAt);
    }

    [Fact]
    public async Task Create_AllowsReusingPlateNumber_WhenPreviousVehicleWasSoftDeleted()
    {
        var deletedVehicle = CreateVehicle(
            "1234ABC",
            deletedAt: new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc));

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Vehicles.Add(deletedVehicle);
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/vehicles",
            CreateVehicleRequest(plateNumber: "1234ABC", internalCode: "VEH-205"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var vehicles = await dbContext.Vehicles
            .OrderBy(vehicle => vehicle.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, vehicles.Count);
        Assert.Equal(1, vehicles.Count(vehicle => vehicle.PlateNumber == "1234ABC" && vehicle.DeletedAt is null));
        Assert.Equal(1, vehicles.Count(vehicle => vehicle.PlateNumber == "1234ABC" && vehicle.DeletedAt is not null));
    }

    private static Vehicle CreateVehicle(
        string plateNumber,
        string? internalCode = null,
        DateTime? deletedAt = null)
    {
        return new Vehicle
        {
            PlateNumber = plateNumber,
            InternalCode = internalCode,
            Brand = "Volvo",
            Model = "FH",
            CapacityKg = 10000,
            CapacityM3 = 50,
            IsActive = true,
            CreatedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
            DeletedAt = deletedAt
        };
    }

    private static object CreateVehicleRequest(
        string plateNumber,
        string? internalCode = null,
        string? brand = "Volvo",
        string? model = "FH",
        decimal? capacityKg = 10000,
        decimal? capacityM3 = 50,
        bool isActive = true)
    {
        return new
        {
            plateNumber,
            internalCode,
            brand,
            model,
            capacityKg,
            capacityM3,
            isActive
        };
    }

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!;
    }
}
