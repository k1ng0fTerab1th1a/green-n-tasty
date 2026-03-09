using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Restaurant.IntegrationTests.Api;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-User-Id", out var userId) || string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(AuthenticateResult.Fail("Missing X-User-Id"));

        var claims = new List<Claim>
        {
            new("sub", userId!),
        };

        if (Request.Headers.TryGetValue("X-Role", out var role) && !string.IsNullOrWhiteSpace(role))
            claims.Add(new Claim("custom:role", role!));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
