using System.Collections.Concurrent;
using System.Security.Claims;
using Orchestrator.Domain;

namespace Orchestrator.Api.Middleware;

/// <summary>
/// Middleware that resolves the authenticated user's ATS access context.
/// For JWT-authenticated requests (not ATS API-key or candidate-session routes),
/// extracts the Auth0 sub and X-Group-Id header, calls the ATS gateway to determine
/// what organizations the user can access, and stores the result in HttpContext.Items["UserContext"].
///
/// Uses a short-lived in-memory cache so that concurrent API calls from the same
/// page load (same user + group) share a single ATS round-trip instead of one each.
/// </summary>
public class UserContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextMiddleware> _logger;

    private static readonly ConcurrentDictionary<string, CachedUserContext> _cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

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

        // Check cache first to avoid redundant ATS round-trips for concurrent requests
        var cacheKey = $"{auth0Sub}:{externalGroupId}";
        if (_cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            context.Items["UserContext"] = cached.UserContext;
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
                userContext.UserName = atsAccess.UserName;
                userContext.IsSuperadmin = atsAccess.IsSuperadmin;
                userContext.IsGroupAdmin = atsAccess.IsGroupAdmin;
                userContext.AdminGroupIds = atsAccess.AdminGroupIds;
                userContext.AccessibleOrganizationIds = atsAccess.AccessibleOrganizations
                    .Select(o => o.Id)
                    .ToList();
            }

            context.Items["UserContext"] = userContext;
            _cache[cacheKey] = new CachedUserContext(userContext, DateTime.UtcNow.Add(CacheTtl));

            // Lazily evict expired entries
            EvictExpiredEntries();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to resolve user context from ATS for auth0Sub={Auth0Sub}, groupId={GroupId}. Proceeding without ATS context.",
                auth0Sub, groupId);

            // IsResolved = false: we know the group but could NOT determine org-level
            // access from the ATS.  Downstream controllers must treat this as
            // "show everything in the group" rather than "show nothing".
            context.Items["UserContext"] = new UserContext
            {
                Auth0Sub = auth0Sub,
                GroupId = groupId,
                IsResolved = false
            };
        }

        await _next(context);
    }

    private static void EvictExpiredEntries()
    {
        var now = DateTime.UtcNow;
        foreach (var key in _cache.Keys)
        {
            if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt <= now)
                _cache.TryRemove(key, out _);
        }
    }

    private sealed record CachedUserContext(UserContext UserContext, DateTime ExpiresAt);
}

public static class UserContextMiddlewareExtensions
{
    public static IApplicationBuilder UseUserContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserContextMiddleware>();
    }
}
