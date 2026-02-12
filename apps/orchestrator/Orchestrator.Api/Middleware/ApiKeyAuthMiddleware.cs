using Orchestrator.Domain;

namespace Orchestrator.Api.Middleware;

/// <summary>
/// Middleware for API key authentication on ATS-facing endpoints
/// </summary>
public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private const string API_KEY_HEADER = "X-API-Key";

    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, DomainFacade domainFacade)
    {
        // Only apply to /api/v1/ats/* endpoints
        if (!context.Request.Path.StartsWithSegments("/api/v1/ats"))
        {
            await _next(context);
            return;
        }

        // Check for API key header
        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API key is missing" });
            return;
        }

        // Validate API key against organizations
        var organization = await domainFacade.GetOrganizationByApiKey(extractedApiKey!);
        if (organization == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        // Store organization ID in context for use in controllers
        context.Items["OrganizationId"] = organization.Id;
        context.Items["Organization"] = organization;

        await _next(context);
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
