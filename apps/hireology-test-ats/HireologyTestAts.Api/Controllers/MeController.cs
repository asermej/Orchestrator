using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.Mappers;
using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Produces("application/json")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public MeController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Current user and list of accessible groups and organizations (for location switcher).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(MeResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<MeResponse>> Get()
    {
        var auth0Sub = GetAuth0Sub();
        if (string.IsNullOrEmpty(auth0Sub)) return Unauthorized();

        var (email, name) = GetEmailAndName();

        var user = await _domainFacade.GetOrCreateUser(auth0Sub, email, name);

        // Superadmins see all groups and organizations; regular users only see what they have access to
        IReadOnlyList<Group> groups;
        IReadOnlyList<Organization> organizations;
        if (user.IsSuperadmin)
        {
            groups = await _domainFacade.GetGroups(excludeTestData: true);
            organizations = await _domainFacade.GetOrganizations(excludeTestData: true);
        }
        else
        {
            groups = await _domainFacade.GetAccessibleGroups(user.Id);
            organizations = await _domainFacade.GetAccessibleOrganizations(user.Id);
        }

        var selectedOrganizationId = await _domainFacade.GetSelectedOrganizationId(user.Id);
        var adminGroupIds = await _domainFacade.GetGroupAdminGroupIds(user.Id);

        return Ok(new MeResponse
        {
            User = UserMapper.ToResource(user),
            IsSuperadmin = user.IsSuperadmin,
            IsGroupAdmin = adminGroupIds.Count > 0 || user.IsSuperadmin,
            AdminGroupIds = adminGroupIds,
            AccessibleGroups = GroupMapper.ToResource(groups),
            AccessibleOrganizations = OrganizationMapper.ToResource(organizations),
            CurrentContext = new MeContextResponse { SelectedOrganizationId = selectedOrganizationId }
        });
    }

    /// <summary>
    /// Get current context (selected organization).
    /// </summary>
    [HttpGet("context")]
    [ProducesResponseType(typeof(MeContextResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<MeContextResponse>> GetContext()
    {
        var auth0Sub = GetAuth0Sub();
        if (string.IsNullOrEmpty(auth0Sub)) return Unauthorized();

        var (email, name) = GetEmailAndName();

        var user = await _domainFacade.GetOrCreateUser(auth0Sub, email, name);

        var selectedOrganizationId = await _domainFacade.GetSelectedOrganizationId(user.Id);
        return Ok(new MeContextResponse { SelectedOrganizationId = selectedOrganizationId });
    }

    /// <summary>
    /// Update the current user's profile (name, email).
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(MeResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<MeResponse>> UpdateMe([FromBody] UpdateMeResource resource)
    {
        var auth0Sub = GetAuth0Sub();
        if (string.IsNullOrEmpty(auth0Sub)) return Unauthorized();

        var (email, name) = GetEmailAndName();

        var user = await _domainFacade.GetOrCreateUser(auth0Sub, email, name);

        var updates = new User
        {
            Email = resource.Email,
            Name = resource.Name
        };
        user = await _domainFacade.UpdateUser(user.Id, updates);

        // Rebuild the full MeResponse so the frontend can update in-place
        IReadOnlyList<Group> groups;
        IReadOnlyList<Organization> organizations;
        if (user.IsSuperadmin)
        {
            groups = await _domainFacade.GetGroups(excludeTestData: true);
            organizations = await _domainFacade.GetOrganizations(excludeTestData: true);
        }
        else
        {
            groups = await _domainFacade.GetAccessibleGroups(user.Id);
            organizations = await _domainFacade.GetAccessibleOrganizations(user.Id);
        }

        var selectedOrganizationId = await _domainFacade.GetSelectedOrganizationId(user.Id);
        var adminGroupIds = await _domainFacade.GetGroupAdminGroupIds(user.Id);

        return Ok(new MeResponse
        {
            User = UserMapper.ToResource(user),
            IsSuperadmin = user.IsSuperadmin,
            IsGroupAdmin = adminGroupIds.Count > 0 || user.IsSuperadmin,
            AdminGroupIds = adminGroupIds,
            AccessibleGroups = GroupMapper.ToResource(groups),
            AccessibleOrganizations = OrganizationMapper.ToResource(organizations),
            CurrentContext = new MeContextResponse { SelectedOrganizationId = selectedOrganizationId }
        });
    }

    /// <summary>
    /// Set current context (selected organization). User must have access to the organization.
    /// </summary>
    [HttpPut("context")]
    [ProducesResponseType(typeof(MeContextResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<MeContextResponse>> SetContext([FromBody] SetContextResource resource)
    {
        var auth0Sub = GetAuth0Sub();
        if (string.IsNullOrEmpty(auth0Sub)) return Unauthorized();

        var (email, name) = GetEmailAndName();

        var user = await _domainFacade.GetOrCreateUser(auth0Sub, email, name);

        await _domainFacade.SetSelectedOrganizationId(user.Id, resource.SelectedOrganizationId);
        return Ok(new MeContextResponse { SelectedOrganizationId = resource.SelectedOrganizationId });
    }

    private string? GetAuth0Sub()
    {
        return User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("sub");
    }

    /// <summary>
    /// Get email and name from JWT claims first, then fall back to X-User-Email / X-User-Name
    /// headers (sent by the frontend from the Auth0 session/ID token, since Auth0 access tokens
    /// for custom APIs don't include email/name claims by default).
    /// </summary>
    private (string? email, string? name) GetEmailAndName()
    {
        var email = User?.FindFirstValue(ClaimTypes.Email)
            ?? User?.FindFirstValue("email");
        var name = User?.FindFirstValue(ClaimTypes.Name)
            ?? User?.FindFirstValue("name");

        // Fallback: frontend sends these from the Auth0 session when claims are missing
        if (string.IsNullOrEmpty(email))
        {
            email = Request.Headers["X-User-Email"].FirstOrDefault();
        }
        if (string.IsNullOrEmpty(name))
        {
            name = Request.Headers["X-User-Name"].FirstOrDefault();
        }

        return (email, name);
    }
}
