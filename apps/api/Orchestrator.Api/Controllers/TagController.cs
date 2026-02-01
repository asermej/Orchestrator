using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for Tag management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class TagController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public TagController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Creates a new tag
    /// </summary>
    /// <param name="resource">The tag data</param>
    /// <returns>The created tag with its ID</returns>
    /// <response code="201">Returns the newly created tag</response>
    /// <response code="400">If the resource is invalid</response>
    /// <response code="409">If a tag with the same name already exists</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(TagResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<TagResource>> Create([FromBody] CreateTagResource resource)
    {
        var tag = TagMapper.ToDomain(resource);
        
        var createdTag = await _domainFacade.CreateTag(tag);

        var response = TagMapper.ToResource(createdTag);
        
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets a tag by ID
    /// </summary>
    /// <param name="id">The ID of the tag</param>
    /// <returns>The tag with the specified ID</returns>
    /// <response code="200">Returns the tag</response>
    /// <response code="404">If the tag is not found</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TagResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TagResource>> GetById(Guid id)
    {
        var tag = await _domainFacade.GetTagById(id);

        if (tag == null)
        {
            return NotFound(new ErrorResponse { Message = $"Tag with ID {id} not found" });
        }

        var response = TagMapper.ToResource(tag);
        return Ok(response);
    }

    /// <summary>
    /// Searches for tags
    /// </summary>
    /// <param name="request">The search criteria with pagination</param>
    /// <returns>A paginated list of tags matching the search criteria</returns>
    /// <response code="200">Returns the list of tags</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<TagResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<TagResource>>> Search([FromQuery] SearchTagRequest request)
    {
        var result = await _domainFacade.SearchTags(
            request.SearchTerm,
            request.PageNumber,
            request.PageSize
        );

        var resourceItems = TagMapper.ToResource(result.Items);
        var response = new PaginatedResponse<TagResource>
        {
            Items = resourceItems,
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Deletes a tag
    /// </summary>
    /// <param name="id">The ID of the tag to delete</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the tag was deleted successfully</response>
    /// <response code="404">If the tag is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _domainFacade.DeleteTag(id);

        if (!result)
        {
            return NotFound(new ErrorResponse { Message = $"Tag with ID {id} not found" });
        }

        return NoContent();
    }
}

