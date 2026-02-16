namespace Orchestrator.Api.Middleware;

/// <summary>
/// Holds the resolved authorization context for the current authenticated user.
/// Populated by UserContextMiddleware and stored in HttpContext.Items["UserContext"].
/// </summary>
public class UserContext
{
    /// <summary>The user's Auth0 sub claim</summary>
    public string Auth0Sub { get; set; } = string.Empty;

    /// <summary>The user's display name from the ATS</summary>
    public string? UserName { get; set; }

    /// <summary>The Orchestrator group ID the user is operating in</summary>
    public Guid GroupId { get; set; }

    /// <summary>Whether the user is a superadmin (sees everything)</summary>
    public bool IsSuperadmin { get; set; }

    /// <summary>Whether the user is a group admin in the ATS</summary>
    public bool IsGroupAdmin { get; set; }

    /// <summary>Organization IDs the user has access to in the current group</summary>
    public IReadOnlyList<Guid> AccessibleOrganizationIds { get; set; } = Array.Empty<Guid>();

    /// <summary>Group IDs where the user is an admin</summary>
    public IReadOnlyList<Guid> AdminGroupIds { get; set; } = Array.Empty<Guid>();

    /// <summary>Whether the user context was successfully resolved from the ATS</summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// Returns true if the user can access a specific organization.
    /// Superadmins and group admins can access all orgs in their group.
    /// </summary>
    public bool CanAccessOrganization(Guid? organizationId)
    {
        if (IsSuperadmin || IsGroupAdmin) return true;
        if (organizationId == null) return true;
        return AccessibleOrganizationIds.Contains(organizationId.Value);
    }
}
