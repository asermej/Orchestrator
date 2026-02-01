using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for Category management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public CategoryController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Creates a new category
    /// </summary>
    /// <param name="resource">The category data</param>
    /// <returns>The created category with its ID</returns>
    /// <response code="201">Returns the newly created category</response>
    /// <response code="400">If the resource is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<CategoryResource>> Create([FromBody] CreateCategoryResource resource)
    {
        var category = CategoryMapper.ToDomain(resource);
        
        var createdCategory = await _domainFacade.CreateCategory(category);

        var response = CategoryMapper.ToResource(createdCategory);
        
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets a category by ID
    /// </summary>
    /// <param name="id">The ID of the category</param>
    /// <returns>The category if found</returns>
    /// <response code="200">Returns the category</response>
    /// <response code="404">If the category is not found</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CategoryResource>> GetById(Guid id)
    {
        var category = await _domainFacade.GetCategoryById(id);
        if (category == null)
        {
            return NotFound($"Category with ID {id} not found");
        }

        var response = CategoryMapper.ToResource(category);
        
        return Ok(response);
    }

    /// <summary>
    /// Searches for categories with pagination
    /// </summary>
    /// <param name="request">The search criteria</param>
    /// <returns>A paginated list of categories</returns>
    /// <response code="200">Returns the paginated results</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<CategoryResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<CategoryResource>>> Search([FromQuery] SearchCategoryRequest request)
    {
        var result = await _domainFacade.SearchCategories(
            request.Name, 
            request.CategoryType,
            request.IsActive,
            request.PageNumber, 
            request.PageSize);

        var response = new PaginatedResponse<CategoryResource>
        {
            Items = CategoryMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Updates a category
    /// </summary>
    /// <param name="id">The ID of the category</param>
    /// <param name="resource">The updated category data</param>
    /// <returns>The updated category</returns>
    /// <response code="200">Returns the updated category</response>
    /// <response code="404">If the category is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CategoryResource), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<CategoryResource>> Update(Guid id, [FromBody] UpdateCategoryResource resource)
    {
        // Get existing category first
        var existingCategory = await _domainFacade.GetCategoryById(id);
        if (existingCategory == null)
        {
            return NotFound($"Category with ID {id} not found");
        }

        // Map update to domain object
        var categoryToUpdate = CategoryMapper.ToDomain(resource, existingCategory);
        
        var updatedCategory = await _domainFacade.UpdateCategory(categoryToUpdate);

        var response = CategoryMapper.ToResource(updatedCategory);
        
        return Ok(response);
    }

    /// <summary>
    /// Deletes a category
    /// </summary>
    /// <param name="id">The ID of the category</param>
    /// <returns>No content on success</returns>
    /// <response code="204">If the category was deleted</response>
    /// <response code="404">If the category is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _domainFacade.DeleteCategory(id);
        if (!deleted)
        {
            return NotFound($"Category with ID {id} not found");
        }

        return NoContent();
    }
}

