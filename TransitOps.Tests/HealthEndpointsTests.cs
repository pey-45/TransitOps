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

    [Fact]
    public async Task Live_ReturnsGeneratedCorrelationId_WhenHeaderIsMissing()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var requestId = payload["meta"]?["requestId"]?.GetValue<string>();

        Assert.False(string.IsNullOrWhiteSpace(requestId));
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        Assert.Equal(requestId, Assert.Single(values));
    }

    [Fact]
    public async Task Live_PreservesSubmittedCorrelationId()
    {
        const string correlationId = "sprint6-correlation-test";

        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health/live");
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal(correlationId, payload["meta"]?["requestId"]?.GetValue<string>());
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        Assert.Equal(correlationId, Assert.Single(values));
    }

    [Fact]
    public async Task ProtectedEndpointError_UsesCorrelationIdInHeaderAndPayload()
    {
        const string correlationId = "sprint6-auth-error";

        using var factory = new TransitOpsApiFactory(useTestAuthentication: false);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/transports");
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("authentication_required", payload["error"]?["code"]?.GetValue<string>());
        Assert.Equal(correlationId, payload["meta"]?["requestId"]?.GetValue<string>());
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        Assert.Equal(correlationId, Assert.Single(values));
    }

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!;
    }
}
