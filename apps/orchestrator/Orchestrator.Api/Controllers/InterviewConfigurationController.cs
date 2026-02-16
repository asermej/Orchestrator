using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;
using Orchestrator.Api.Middleware;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for Interview Configuration management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class InterviewConfigurationController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public InterviewConfigurationController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    private UserContext? GetUserContext()
        => HttpContext.Items.TryGetValue("UserContext", out var ctx) ? ctx as UserContext : null;

    /// <summary>
    /// Creates a new interview configuration
    /// </summary>
    /// <param name="resource">The configuration data</param>
    /// <returns>The created configuration with its ID</returns>
    /// <response code="201">Returns the newly created configuration</response>
    /// <response code="400">If the resource is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(InterviewConfigurationResource), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<InterviewConfigurationResource>> Create([FromBody] CreateInterviewConfigurationResource resource)
    {
        var userContext = GetUserContext();
        var groupId = userContext?.GroupId ?? resource.GroupId;

        var config = InterviewConfigurationMapper.ToDomain(resource, groupId);
        var createdConfig = await _domainFacade.CreateInterviewConfiguration(config);

        var response = InterviewConfigurationMapper.ToResource(createdConfig);
        
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets an interview configuration by ID
    /// </summary>
    /// <param name="id">The ID of the configuration</param>
    /// <returns>The configuration if found</returns>
    /// <response code="200">Returns the configuration</response>
    /// <response code="404">If the configuration is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InterviewConfigurationResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewConfigurationResource>> GetById(Guid id)
    {
        var config = await _domainFacade.GetInterviewConfigurationById(id);
        
        if (config == null)
        {
            return NotFound($"Interview configuration with ID {id} not found");
        }

        // Load the agent
        var agent = await _domainFacade.GetAgentById(config.AgentId);
        config.Agent = agent;

        // Load the interview guide with questions
        var guide = await _domainFacade.GetInterviewGuideByIdWithQuestions(config.InterviewGuideId);
        config.InterviewGuide = guide;

        var response = InterviewConfigurationMapper.ToResource(config);
        
        return Ok(response);
    }

    /// <summary>
    /// Searches for interview configurations with pagination
    /// </summary>
    /// <param name="request">The search criteria</param>
    /// <returns>A paginated list of configurations</returns>
    /// <response code="200">Returns the paginated results</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<InterviewConfigurationResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<InterviewConfigurationResource>>> Search([FromQuery] SearchInterviewConfigurationRequest request)
    {
        var userContext = GetUserContext();
        var groupId = request.GroupId ?? userContext?.GroupId;
        var orgFilter = (userContext is { IsResolved: true, IsSuperadmin: false, IsGroupAdmin: false })
            ? userContext.AccessibleOrganizationIds
            : null;

        var result = await _domainFacade.SearchInterviewConfigurations(
            groupId,
            request.AgentId,
            request.Name,
            request.IsActive,
            request.SortBy,
            request.PageNumber, 
            request.PageSize,
            orgFilter);

        var response = new PaginatedResponse<InterviewConfigurationResource>
        {
            Items = InterviewConfigurationMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Updates an interview configuration
    /// </summary>
    /// <param name="id">The ID of the configuration</param>
    /// <param name="resource">The updated configuration data</param>
    /// <returns>The updated configuration</returns>
    /// <response code="200">Returns the updated configuration</response>
    /// <response code="404">If the configuration is not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(InterviewConfigurationResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewConfigurationResource>> Update(Guid id, [FromBody] UpdateInterviewConfigurationResource resource)
    {
        // Get existing configuration first
        var existingConfig = await _domainFacade.GetInterviewConfigurationById(id);
        if (existingConfig == null)
        {
            return NotFound($"Interview configuration with ID {id} not found");
        }

        // Map update to domain object
        var configToUpdate = InterviewConfigurationMapper.ToDomain(resource, existingConfig);
        var updatedConfig = await _domainFacade.UpdateInterviewConfiguration(configToUpdate);

        var response = InterviewConfigurationMapper.ToResource(updatedConfig);
        
        return Ok(response);
    }

    /// <summary>
    /// Deletes an interview configuration
    /// </summary>
    /// <param name="id">The ID of the configuration</param>
    /// <returns>No content on success</returns>
    /// <response code="204">If the configuration was deleted</response>
    /// <response code="404">If the configuration is not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _domainFacade.DeleteInterviewConfiguration(id);
        if (!deleted)
        {
            return NotFound($"Interview configuration with ID {id} not found");
        }

        return NoContent();
    }
}
