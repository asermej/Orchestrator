using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Api.Mappers;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Controllers;

/// <summary>
/// External API endpoints for server-to-server integration (e.g., Orchestrator calling ATS).
/// Uses API key authentication via X-API-Key header.
/// </summary>
[ApiController]
[Route("api/v1/external")]
[Produces("application/json")]
public class ExternalController : ControllerBase
{
    private readonly DomainFacade _domainFacade;
    private readonly IConfiguration _configuration;

    public ExternalController(DomainFacade domainFacade, IConfiguration configuration)
    {
        _domainFacade = domainFacade;
        _configuration = configuration;
    }

    /// <summary>
    /// Returns the groups and organizations a user has access to, identified by Auth0 sub.
    /// Used by the Orchestrator to determine what data a user should see.
    /// </summary>
    [HttpGet("user-access")]
    [ProducesResponseType(typeof(ExternalUserAccessResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ExternalUserAccessResponse>> GetUserAccess([FromQuery] string auth0Sub)
    {
        if (!ValidateApiKey())
            return Unauthorized(new { message = "Invalid or missing API key" });

        if (string.IsNullOrWhiteSpace(auth0Sub))
            return BadRequest(new { message = "auth0Sub query parameter is required" });

        var user = await _domainFacade.GetUserByAuth0Sub(auth0Sub);
        if (user == null)
            return NotFound(new { message = $"User with auth0Sub '{auth0Sub}' not found" });

        var groups = user.IsSuperadmin
            ? await _domainFacade.GetGroups(excludeTestData: true)
            : await _domainFacade.GetAccessibleGroups(user.Id);

        var organizations = user.IsSuperadmin
            ? await _domainFacade.GetOrganizations(excludeTestData: true)
            : await _domainFacade.GetAccessibleOrganizations(user.Id);

        var adminGroupIds = await _domainFacade.GetGroupAdminGroupIds(user.Id);

        return Ok(new ExternalUserAccessResponse
        {
            UserId = user.Id,
            Auth0Sub = user.Auth0Sub,
            UserName = user.Name,
            IsSuperadmin = user.IsSuperadmin,
            IsGroupAdmin = adminGroupIds.Count > 0 || user.IsSuperadmin,
            AdminGroupIds = adminGroupIds,
            AccessibleGroups = groups.Select(g => new ExternalGroupInfo
            {
                Id = g.Id,
                Name = g.Name
            }).ToList(),
            AccessibleOrganizations = organizations.Select(o => new ExternalOrganizationInfo
            {
                Id = o.Id,
                GroupId = o.GroupId,
                Name = o.Name
            }).ToList()
        });
    }

    /// <summary>
    /// Returns all organizations in a group. Used by the Orchestrator to know what organizations exist.
    /// </summary>
    [HttpGet("organizations")]
    [ProducesResponseType(typeof(IReadOnlyList<ExternalOrganizationInfo>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<IReadOnlyList<ExternalOrganizationInfo>>> GetOrganizations([FromQuery] Guid? groupId)
    {
        if (!ValidateApiKey())
            return Unauthorized(new { message = "Invalid or missing API key" });

        var organizations = await _domainFacade.GetOrganizations(groupId, excludeTestData: true);

        var result = organizations.Select(o => new ExternalOrganizationInfo
        {
            Id = o.Id,
            GroupId = o.GroupId,
            Name = o.Name
        }).ToList();

        return Ok(result);
    }

    private bool ValidateApiKey()
    {
        var expectedKey = _configuration["HireologyAts:ApiKey"];
        if (string.IsNullOrEmpty(expectedKey))
            return false;

        var providedKey = Request.Headers["X-API-Key"].FirstOrDefault();
        return !string.IsNullOrEmpty(providedKey) && providedKey == expectedKey;
    }
}

/// <summary>
/// Response model for external user-access endpoint
/// </summary>
public class ExternalUserAccessResponse
{
    public Guid UserId { get; set; }
    public string Auth0Sub { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public bool IsSuperadmin { get; set; }
    public bool IsGroupAdmin { get; set; }
    public IReadOnlyList<Guid> AdminGroupIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<ExternalGroupInfo> AccessibleGroups { get; set; } = Array.Empty<ExternalGroupInfo>();
    public IReadOnlyList<ExternalOrganizationInfo> AccessibleOrganizations { get; set; } = Array.Empty<ExternalOrganizationInfo>();
}

public class ExternalGroupInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ExternalOrganizationInfo
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
}
