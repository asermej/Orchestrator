using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for Organization management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class OrganizationController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public OrganizationController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Creates a new organization
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrganizationResource), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<OrganizationResource>> Create([FromBody] CreateOrganizationResource resource)
    {
        var organization = OrganizationMapper.ToDomain(resource);
        var created = await _domainFacade.CreateOrganization(organization);
        var response = OrganizationMapper.ToResource(created);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets an organization by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrganizationResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<OrganizationResource>> GetById(Guid id)
    {
        var organization = await _domainFacade.GetOrganizationById(id);
        if (organization == null)
        {
            return NotFound($"Organization with ID {id} not found");
        }
        return Ok(OrganizationMapper.ToResource(organization));
    }

    /// <summary>
    /// Searches for organizations
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<OrganizationResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<OrganizationResource>>> Search([FromQuery] SearchOrganizationRequest request)
    {
        var result = await _domainFacade.SearchOrganizations(
            request.Name,
            request.IsActive,
            request.PageNumber,
            request.PageSize);

        var response = new PaginatedResponse<OrganizationResource>
        {
            Items = OrganizationMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Updates an organization
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(OrganizationResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<OrganizationResource>> Update(Guid id, [FromBody] UpdateOrganizationResource resource)
    {
        var existing = await _domainFacade.GetOrganizationById(id);
        if (existing == null)
        {
            return NotFound($"Organization with ID {id} not found");
        }

        var organization = OrganizationMapper.ToDomain(resource, existing);
        var updated = await _domainFacade.UpdateOrganization(organization);
        return Ok(OrganizationMapper.ToResource(updated));
    }

    /// <summary>
    /// Deletes an organization
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _domainFacade.DeleteOrganization(id);
        if (!deleted)
        {
            return NotFound($"Organization with ID {id} not found");
        }
        return NoContent();
    }
}
