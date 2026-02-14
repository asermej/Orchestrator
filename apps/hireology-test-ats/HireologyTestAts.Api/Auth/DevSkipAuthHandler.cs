using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HireologyTestAts.Api.Auth;

/// <summary>
/// When Auth0 is not configured, this handler creates a synthetic dev user
/// so the app is fully usable without Auth0 credentials.
/// </summary>
public class DevSkipAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevSkipAuth";
    public const string DevSub = "dev|local-admin";
    public const string DevEmail = "admin@localhost";
    public const string DevName = "Local Dev User";

    public DevSkipAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, DevSub),
            new Claim("sub", DevSub),
            new Claim(ClaimTypes.Email, DevEmail),
            new Claim("email", DevEmail),
            new Claim(ClaimTypes.Name, DevName),
            new Claim("name", DevName),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
