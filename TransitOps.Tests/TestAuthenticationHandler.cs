using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransitOps.Api.Domain.Enums;
using TransitOps.Api.Security;

namespace TransitOps.Tests;

public sealed class TestAuthenticationOptions : AuthenticationSchemeOptions
{
    public Guid UserId { get; set; } = TransitOpsApiFactory.DefaultAuthenticatedUserId;

    public string Username { get; set; } = TransitOpsApiFactory.DefaultAuthenticatedUsername;

    public string Email { get; set; } = TransitOpsApiFactory.DefaultAuthenticatedEmail;

    public UserRole Role { get; set; } = UserRole.Admin;
}

public sealed class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationOptions>
{
    public const string SchemeName = "TransitOpsTest";

    public TestAuthenticationHandler(
        IOptionsMonitor<TestAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("sub", Options.UserId.ToString()),
            new Claim("unique_name", Options.Username),
            new Claim("email", Options.Email),
            new Claim("role", Options.Role.ToClaimValue())
        };

        var identity = new ClaimsIdentity(claims, SchemeName, "unique_name", "role");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
