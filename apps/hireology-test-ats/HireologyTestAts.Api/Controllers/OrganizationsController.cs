using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.Auth;
using HireologyTestAts.Api.Mappers;
using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public OrganizationsController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizationResource>), 200)]
    public async Task<ActionResult<IReadOnlyList<OrganizationResource>>> List([FromQuery] Guid? groupId)
    {
        var orgs = await _domainFacade.GetOrganizations(groupId);
        return Ok(OrganizationMapper.ToResource(orgs));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrganizationResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<OrganizationResource>> GetById(Guid id)
    {
        var org = await _domainFacade.GetOrganizationById(id);
        return Ok(OrganizationMapper.ToResource(org));
    }

    /// <summary>
    /// Returns all organizations in a group as a flat list for client-side tree building.
    /// </summary>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizationResource>), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<IReadOnlyList<OrganizationResource>>> GetTree([FromQuery] Guid groupId)
    {
        if (groupId == Guid.Empty)
            return BadRequest(new { Message = "groupId is required" });

        var orgs = await _domainFacade.GetOrganizationTree(groupId);
        return Ok(OrganizationMapper.ToResource(orgs));
    }

    [HttpPost]
    [SuperadminRequired]
    [ProducesResponseType(typeof(OrganizationResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<OrganizationResource>> Create([FromBody] CreateOrganizationResource resource)
    {
        var org = OrganizationMapper.ToDomain(resource);
        var created = await _domainFacade.CreateOrganization(org);
        var response = OrganizationMapper.ToResource(created);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [SuperadminRequired]
    [ProducesResponseType(typeof(OrganizationResource), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<OrganizationResource>> Update(Guid id, [FromBody] UpdateOrganizationResource resource)
    {
        var updates = OrganizationMapper.ToDomain(resource);
        var updated = await _domainFacade.UpdateOrganization(id, updates);
        return Ok(OrganizationMapper.ToResource(updated));
    }

    [HttpPost("{id:guid}/move")]
    [SuperadminRequired]
    [ProducesResponseType(typeof(OrganizationResource), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<OrganizationResource>> Move(Guid id, [FromBody] MoveOrganizationResource resource)
    {
        var updated = await _domainFacade.MoveOrganization(id, resource.NewParentOrganizationId);
        return Ok(OrganizationMapper.ToResource(updated));
    }

    [HttpDelete("{id:guid}")]
    [SuperadminRequired]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _domainFacade.DeleteOrganization(id);
        return NoContent();
    }
}
