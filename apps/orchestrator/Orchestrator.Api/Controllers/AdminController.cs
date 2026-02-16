using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.Middleware;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Administrative endpoints for group admins.
/// Requires an authenticated user with group admin privileges.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Produces("application/json")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public AdminController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    private UserContext? GetUserContext()
        => HttpContext.Items.TryGetValue("UserContext", out var ctx) ? ctx as UserContext : null;

    /// <summary>
    /// Gets a summary of orphaned entities in the current group.
    /// Orphaned entities have an organization_id that no longer exists in the ATS.
    /// Only accessible to group admins and superadmins.
    /// </summary>
    [HttpGet("orphaned-entities")]
    [ProducesResponseType(typeof(OrphanedEntitySummaryResource), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<OrphanedEntitySummaryResource>> GetOrphanedEntities()
    {
        var userContext = GetUserContext();
        if (userContext == null || !userContext.IsResolved)
        {
            return Unauthorized(new { message = "User context not resolved. Ensure X-Group-Id header is set." });
        }

        if (!userContext.IsGroupAdmin && !userContext.IsSuperadmin)
        {
            return Forbid();
        }

        var summary = await _domainFacade.GetOrphanedEntitySummary(
            userContext.GroupId,
            userContext.AccessibleOrganizationIds);

        return Ok(new OrphanedEntitySummaryResource
        {
            OrphanedAgentCount = summary.OrphanedAgentCount,
            OrphanedInterviewGuideCount = summary.OrphanedInterviewGuideCount,
            OrphanedInterviewConfigurationCount = summary.OrphanedInterviewConfigurationCount,
            OrphanedJobCount = summary.OrphanedJobCount,
            OrphanedApplicantCount = summary.OrphanedApplicantCount,
            TotalOrphanedCount = summary.TotalOrphanedCount,
            OrphanedOrganizationIds = summary.OrphanedOrganizationIds
        });
    }
}

public class OrphanedEntitySummaryResource
{
    public int OrphanedAgentCount { get; set; }
    public int OrphanedInterviewGuideCount { get; set; }
    public int OrphanedInterviewConfigurationCount { get; set; }
    public int OrphanedJobCount { get; set; }
    public int OrphanedApplicantCount { get; set; }
    public int TotalOrphanedCount { get; set; }
    public IReadOnlyList<Guid> OrphanedOrganizationIds { get; set; } = Array.Empty<Guid>();
}
