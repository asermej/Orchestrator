using System.Security.Claims;
using Orchestrator.Domain;

namespace Orchestrator.Api.Middleware;

/// <summary>
/// Middleware that resolves the authenticated user's ATS access context.
/// For JWT-authenticated requests (not ATS API-key or candidate-session routes),
/// extracts the Auth0 sub and X-Group-Id header, calls the ATS gateway to determine
/// what organizations the user can access, and stores the result in HttpContext.Items["UserContext"].
/// </summary>
public class UserContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextMiddleware> _logger;

    public UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, DomainFacade domainFacade)
    {
        // Skip for ATS server-to-server endpoints (use API key auth instead)
        if (context.Request.Path.StartsWithSegments("/api/v1/ats"))
        {
            await _next(context);
            return;
        }

        // Skip for candidate-facing endpoints
        if (context.Request.Path.StartsWithSegments("/api/v1/candidate"))
        {
            await _next(context);
            return;
        }

        // Skip for non-authenticated requests
        var auth0Sub = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User?.FindFirstValue("sub");

        if (string.IsNullOrEmpty(auth0Sub))
        {
            await _next(context);
            return;
        }

        // Get group context from header (sent by Orchestrator.Web)
        // X-Group-Id contains the ATS external group ID; resolve to internal Orchestrator group
        var groupIdHeader = context.Request.Headers["X-Group-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(groupIdHeader) || !Guid.TryParse(groupIdHeader, out var externalGroupId))
        {
            await _next(context);
            return;
        }

        // Resolve external ATS group ID to internal Orchestrator group
        var group = await domainFacade.GetGroupByExternalGroupId(externalGroupId);
        var groupId = group?.Id ?? Guid.Empty;

        if (groupId == Guid.Empty)
        {
            _logger.LogWarning(
                "No Orchestrator group found for external group ID {ExternalGroupId}. Proceeding without ATS context.",
                externalGroupId);
            await _next(context);
            return;
        }

        try
        {
            var atsAccess = await domainFacade.GetUserAccessFromAts(groupId, auth0Sub);

            var userContext = new UserContext
            {
                Auth0Sub = auth0Sub,
                GroupId = groupId,
                IsResolved = true
            };

            if (atsAccess != null)
            {
                userContext.IsSuperadmin = atsAccess.IsSuperadmin;
                userContext.IsGroupAdmin = atsAccess.IsGroupAdmin;
                userContext.AdminGroupIds = atsAccess.AdminGroupIds;
                userContext.AccessibleOrganizationIds = atsAccess.AccessibleOrganizations
                    .Select(o => o.Id)
                    .ToList();
            }

            context.Items["UserContext"] = userContext;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to resolve user context from ATS for auth0Sub={Auth0Sub}, groupId={GroupId}. Proceeding without ATS context.",
                auth0Sub, groupId);

            context.Items["UserContext"] = new UserContext
            {
                Auth0Sub = auth0Sub,
                GroupId = groupId,
                IsResolved = true
            };
        }

        await _next(context);
    }
}

public static class UserContextMiddlewareExtensions
{
    public static IApplicationBuilder UseUserContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserContextMiddleware>();
    }
}
