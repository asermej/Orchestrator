using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for User management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public UserController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="resource">The user data</param>
    /// <returns>The created user with its ID</returns>
    /// <response code="201">Returns the newly created user</response>
    /// <response code="400">If the resource is invalid</response>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResource), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<UserResource>> Create([FromBody] CreateUserResource resource)
    {
        var user = UserMapper.ToDomain(resource);
        var createdUser = await _domainFacade.CreateUser(user);

        var response = UserMapper.ToResource(createdUser);
        
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="id">The ID of the user</param>
    /// <returns>The user if found</returns>
    /// <response code="200">Returns the user</response>
    /// <response code="404">If the user is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserResource>> GetById(Guid id)
    {
        var user = await _domainFacade.GetUserById(id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        var response = UserMapper.ToResource(user);
        
        return Ok(response);
    }

    /// <summary>
    /// Gets a user by Auth0 Sub
    /// </summary>
    /// <param name="auth0Sub">The Auth0 Sub of the user</param>
    /// <returns>The user if found</returns>
    /// <response code="200">Returns the user</response>
    /// <response code="404">If the user is not found</response>
    [HttpGet("by-auth0-sub/{auth0Sub}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserResource>> GetByAuth0Sub(string auth0Sub)
    {
        var user = await _domainFacade.GetUserByAuth0Sub(auth0Sub);
        if (user == null)
        {
            return NotFound($"User with Auth0 Sub {auth0Sub} not found");
        }

        var response = UserMapper.ToResource(user);
        
        return Ok(response);
    }

    /// <summary>
    /// Searches for users with pagination
    /// </summary>
    /// <param name="request">The search criteria</param>
    /// <returns>A paginated list of users</returns>
    /// <response code="200">Returns the paginated results</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<UserResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<UserResource>>> Search([FromQuery] SearchUserRequest request)
    {
        var result = await _domainFacade.SearchUsers(
            request.Phone, 
            request.Email, 
            request.LastName, 
            request.PageNumber, 
            request.PageSize);

        var response = new PaginatedResponse<UserResource>
        {
            Items = UserMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Updates a user
    /// </summary>
    /// <param name="id">The ID of the user</param>
    /// <param name="resource">The updated user data</param>
    /// <returns>The updated user</returns>
    /// <response code="200">Returns the updated user</response>
    /// <response code="404">If the user is not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserResource>> Update(Guid id, [FromBody] UpdateUserResource resource)
    {
        // Get existing user first
        var existingUser = await _domainFacade.GetUserById(id);
        if (existingUser == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        // Map update to domain object
        var userToUpdate = UserMapper.ToDomain(resource, existingUser);
        var updatedUser = await _domainFacade.UpdateUser(userToUpdate);

        var response = UserMapper.ToResource(updatedUser);
        
        return Ok(response);
    }

    /// <summary>
    /// Deletes a user
    /// </summary>
    /// <param name="id">The ID of the user</param>
    /// <returns>No content on success</returns>
    /// <response code="204">If the user was deleted</response>
    /// <response code="404">If the user is not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _domainFacade.DeleteUser(id);
        if (!deleted)
        {
            return NotFound($"User with ID {id} not found");
        }

        return NoContent();
    }
} 