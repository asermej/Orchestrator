using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.Models;
using HireologyTestAts.Api.Services;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/me")]
[Produces("application/json")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly UserAccessService _userAccess;
    private readonly UserSessionsRepository _sessions;

    public MeController(ICurrentUserService currentUser, UserAccessService userAccess, UserSessionsRepository sessions)
    {
        _currentUser = currentUser;
        _userAccess = userAccess;
        _sessions = sessions;
    }

    /// <summary>
    /// Current user and list of accessible groups and organizations (for location switcher).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(MeResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken ct = default)
    {
        var user = await _currentUser.GetCurrentUserAsync(ct);
        if (user == null) return Unauthorized();

        var groups = await _userAccess.GetAccessibleGroupsAsync(user.Id, ct);
        var organizations = await _userAccess.GetAccessibleOrganizationsAsync(user.Id, ct);
        var selectedOrganizationId = await _sessions.GetSelectedOrganizationIdAsync(user.Id, ct);

        return Ok(new MeResponse
        {
            User = user,
            AccessibleGroups = groups,
            AccessibleOrganizations = organizations,
            CurrentContext = new MeContextResponse { SelectedOrganizationId = selectedOrganizationId }
        });
    }

    /// <summary>
    /// Get current context (selected organization).
    /// </summary>
    [HttpGet("context")]
    [ProducesResponseType(typeof(MeContextResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<MeContextResponse>> GetContext(CancellationToken ct = default)
    {
        var user = await _currentUser.GetCurrentUserAsync(ct);
        if (user == null) return Unauthorized();

        var selectedOrganizationId = await _sessions.GetSelectedOrganizationIdAsync(user.Id, ct);
        return Ok(new MeContextResponse { SelectedOrganizationId = selectedOrganizationId });
    }

    /// <summary>
    /// Set current context (selected organization). User must have access to the organization.
    /// </summary>
    [HttpPut("context")]
    [ProducesResponseType(typeof(MeContextResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<MeContextResponse>> SetContext([FromBody] SetContextRequest request, CancellationToken ct = default)
    {
        var user = await _currentUser.GetCurrentUserAsync(ct);
        if (user == null) return Unauthorized();

        if (request.SelectedOrganizationId.HasValue)
        {
            var canAccess = await _userAccess.CanAccessOrganizationAsync(user.Id, request.SelectedOrganizationId.Value, ct);
            if (!canAccess)
                return BadRequest("You do not have access to this organization.");
        }

        await _sessions.SetSelectedOrganizationIdAsync(user.Id, request.SelectedOrganizationId, ct);
        return Ok(new MeContextResponse { SelectedOrganizationId = request.SelectedOrganizationId });
    }
}

public class MeResponse
{
    public UserItem User { get; set; } = null!;
    public IReadOnlyList<GroupItem> AccessibleGroups { get; set; } = Array.Empty<GroupItem>();
    public IReadOnlyList<OrganizationItem> AccessibleOrganizations { get; set; } = Array.Empty<OrganizationItem>();
    public MeContextResponse CurrentContext { get; set; } = null!;
}

public class MeContextResponse
{
    public Guid? SelectedOrganizationId { get; set; }
}

public class SetContextRequest
{
    public Guid? SelectedOrganizationId { get; set; }
}
