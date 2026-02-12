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

        var email = User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");
        var name = User?.FindFirstValue(ClaimTypes.Name) ?? User?.FindFirstValue("name");

        var user = await _domainFacade.GetOrCreateUser(auth0Sub, email, name);

        var groups = await _domainFacade.GetAccessibleGroups(user.Id);
        var organizations = await _domainFacade.GetAccessibleOrganizations(user.Id);
        var selectedOrganizationId = await _domainFacade.GetSelectedOrganizationId(user.Id);

        return Ok(new MeResponse
        {
            User = UserMapper.ToResource(user),
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

        var email = User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");
        var name = User?.FindFirstValue(ClaimTypes.Name) ?? User?.FindFirstValue("name");

        var user = await _domainFacade.GetOrCreateUser(auth0Sub, email, name);

        var selectedOrganizationId = await _domainFacade.GetSelectedOrganizationId(user.Id);
        return Ok(new MeContextResponse { SelectedOrganizationId = selectedOrganizationId });
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

        var email = User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");
        var name = User?.FindFirstValue(ClaimTypes.Name) ?? User?.FindFirstValue("name");

        var user = await _domainFacade.GetOrCreateUser(auth0Sub, email, name);

        await _domainFacade.SetSelectedOrganizationId(user.Id, resource.SelectedOrganizationId);
        return Ok(new MeContextResponse { SelectedOrganizationId = resource.SelectedOrganizationId });
    }

    private string? GetAuth0Sub()
    {
        return User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("sub");
    }
}
