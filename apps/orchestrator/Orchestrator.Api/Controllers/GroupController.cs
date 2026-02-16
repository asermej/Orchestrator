using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for Group management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public GroupController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Creates a new group
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GroupResource), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<GroupResource>> Create([FromBody] CreateGroupResource resource)
    {
        var group = GroupMapper.ToDomain(resource);
        var created = await _domainFacade.CreateGroup(group);
        var response = GroupMapper.ToResource(created);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets a group by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GroupResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<GroupResource>> GetById(Guid id)
    {
        var group = await _domainFacade.GetGroupById(id);
        if (group == null)
        {
            return NotFound($"Group with ID {id} not found");
        }
        return Ok(GroupMapper.ToResource(group));
    }

    /// <summary>
    /// Gets a group by its external (ATS) group ID
    /// </summary>
    [HttpGet("by-external-id/{externalGroupId}")]
    [ProducesResponseType(typeof(GroupResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<GroupResource>> GetByExternalGroupId(Guid externalGroupId)
    {
        var group = await _domainFacade.GetGroupByExternalGroupId(externalGroupId);
        if (group == null)
        {
            return NotFound($"Group with external ID {externalGroupId} not found");
        }
        return Ok(GroupMapper.ToResource(group));
    }

    /// <summary>
    /// Searches for groups
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<GroupResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<GroupResource>>> Search([FromQuery] SearchGroupRequest request)
    {
        var result = await _domainFacade.SearchGroups(
            request.Name,
            request.IsActive,
            request.PageNumber,
            request.PageSize);

        var response = new PaginatedResponse<GroupResource>
        {
            Items = GroupMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Updates a group
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(GroupResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<GroupResource>> Update(Guid id, [FromBody] UpdateGroupResource resource)
    {
        var existing = await _domainFacade.GetGroupById(id);
        if (existing == null)
        {
            return NotFound($"Group with ID {id} not found");
        }

        var group = GroupMapper.ToDomain(resource, existing);
        var updated = await _domainFacade.UpdateGroup(group);
        return Ok(GroupMapper.ToResource(updated));
    }

    /// <summary>
    /// Deletes a group
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _domainFacade.DeleteGroup(id);
        if (!deleted)
        {
            return NotFound($"Group with ID {id} not found");
        }
        return NoContent();
    }
}
