using System.Net;
using System.Net.Http.Json;
using TransitOps.Tests.Support;

namespace TransitOps.Tests.Controllers;

public sealed class HealthControllerTests
{
    [Fact]
    public async Task Get_returns_healthy_without_authentication()
    {
        using var factory = new TransitOpsApiFactory();
        using var response = await factory.CreateClient().GetAsync("/api/v1/health");
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("healthy", payload?.Status);
    }

    private sealed record HealthResponse(string Status);
}
