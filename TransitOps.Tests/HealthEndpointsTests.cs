using System.Net;
using System.Text.Json.Nodes;

namespace TransitOps.Tests;

public sealed class HealthEndpointsTests
{
    [Fact]
    public async Task Live_ReturnsOkPayload()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("live", payload["data"]?["status"]?.GetValue<string>());
        Assert.Equal("TransitOps.Api", payload["data"]?["service"]?.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(payload["meta"]?["requestId"]?.GetValue<string>()));
    }

    [Fact]
    public async Task Ready_ReturnsOkPayload_WhenDatabaseIsAvailable()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("ready", payload["data"]?["status"]?.GetValue<string>());
        Assert.True(payload["data"]?["databaseConnectionAvailable"]?.GetValue<bool>());
        Assert.False(string.IsNullOrWhiteSpace(payload["meta"]?["requestId"]?.GetValue<string>()));
    }

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!;
    }
}
