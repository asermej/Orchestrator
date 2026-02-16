using Orchestrator.Domain;

namespace Orchestrator.Api.Middleware;

/// <summary>
/// Middleware for API key authentication on ATS-facing endpoints.
/// Supports two authentication mechanisms:
/// 1. X-API-Key header: resolves to a specific group (standard flow)
/// 2. X-Bootstrap-Secret header: allows group creation when no API key exists yet
/// </summary>
public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private const string API_KEY_HEADER = "X-API-Key";
    private const string BOOTSTRAP_SECRET_HEADER = "X-Bootstrap-Secret";

    public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context, DomainFacade domainFacade)
    {
        // Only apply to /api/v1/ats/* endpoints
        if (!context.Request.Path.StartsWithSegments("/api/v1/ats"))
        {
            await _next(context);
            return;
        }

        // For POST /api/v1/ats/groups, also accept bootstrap secret (for first-time group creation)
        if (context.Request.Path.Equals("/api/v1/ats/groups", StringComparison.OrdinalIgnoreCase)
            && context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            if (await TryAuthenticateWithApiKey(context, domainFacade)
                || TryAuthenticateWithBootstrapSecret(context))
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Valid API key or bootstrap secret is required" });
            return;
        }

        // Standard API key authentication for all other ATS endpoints
        if (!await TryAuthenticateWithApiKey(context, domainFacade))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API key is missing or invalid" });
            return;
        }

        await _next(context);
    }

    private static async Task<bool> TryAuthenticateWithApiKey(HttpContext context, DomainFacade domainFacade)
    {
        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
            return false;

        var group = await domainFacade.GetGroupByApiKey(extractedApiKey!);
        if (group == null)
            return false;

        context.Items["GroupId"] = group.Id;
        context.Items["Group"] = group;
        return true;
    }

    private bool TryAuthenticateWithBootstrapSecret(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(BOOTSTRAP_SECRET_HEADER, out var extractedSecret))
            return false;

        var configuredSecret = _configuration["BootstrapSecret"];
        if (string.IsNullOrEmpty(configuredSecret))
            return false;

        return string.Equals(extractedSecret, configuredSecret, StringComparison.Ordinal);
    }
}

/// <summary>
/// Extension methods for API key auth middleware
/// </summary>
public static class ApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
