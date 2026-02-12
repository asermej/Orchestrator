using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for AI Interviewer Agent management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class AgentController : ControllerBase
{
    private readonly DomainFacade _domainFacade;
    private readonly ILogger<AgentController> _logger;
    private readonly IMemoryCache _cache;

    public AgentController(DomainFacade domainFacade, ILogger<AgentController> logger, IMemoryCache cache)
    {
        _domainFacade = domainFacade;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Creates a new agent
    /// </summary>
    /// <param name="resource">The agent data</param>
    /// <returns>The created agent with its ID</returns>
    /// <response code="201">Returns the newly created agent</response>
    /// <response code="400">If the resource is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(AgentResource), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<AgentResource>> Create([FromBody] CreateAgentResource resource)
    {
        // Resolve organization ID - use provided or get/create default
        var organizationId = resource.OrganizationId ?? await GetOrCreateDefaultOrganizationAsync();
        
        var agent = AgentMapper.ToDomain(resource, organizationId);
        var createdAgent = await _domainFacade.CreateAgent(agent);

        var response = AgentMapper.ToResource(createdAgent);
        
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets or creates a default organization for agents when none is specified
    /// </summary>
    private async Task<Guid> GetOrCreateDefaultOrganizationAsync()
    {
        const string defaultOrgName = "Default Organization";
        
        // Search for existing default organization
        var existingOrgs = await _domainFacade.SearchOrganizations(defaultOrgName, true, 1, 1);
        if (existingOrgs.Items.Any())
        {
            return existingOrgs.Items.First().Id;
        }
        
        // Create default organization if it doesn't exist
        var defaultOrg = new Organization
        {
            Name = defaultOrgName,
            IsActive = true
        };
        var created = await _domainFacade.CreateOrganization(defaultOrg);
        return created.Id;
    }

    /// <summary>
    /// Gets an agent by ID
    /// </summary>
    /// <param name="id">The ID of the agent</param>
    /// <returns>The agent if found</returns>
    /// <response code="200">Returns the agent</response>
    /// <response code="404">If the agent is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AgentResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<AgentResource>> GetById(Guid id)
    {
        var agent = await _domainFacade.GetAgentById(id);
        if (agent == null)
        {
            return NotFound($"Agent with ID {id} not found");
        }

        var response = AgentMapper.ToResource(agent);
        
        return Ok(response);
    }

    /// <summary>
    /// Searches for agents with pagination
    /// </summary>
    /// <param name="request">The search criteria</param>
    /// <returns>A paginated list of agents</returns>
    /// <response code="200">Returns the paginated results</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<AgentResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<AgentResource>>> Search([FromQuery] SearchAgentRequest request)
    {
        var result = await _domainFacade.SearchAgents(
            request.OrganizationId,
            request.DisplayName,
            request.CreatedBy,
            request.SortBy,
            request.PageNumber, 
            request.PageSize);

        var response = new PaginatedResponse<AgentResource>
        {
            Items = AgentMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Updates an agent
    /// </summary>
    /// <param name="id">The ID of the agent</param>
    /// <param name="resource">The updated agent data</param>
    /// <returns>The updated agent</returns>
    /// <response code="200">Returns the updated agent</response>
    /// <response code="404">If the agent is not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AgentResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<AgentResource>> Update(Guid id, [FromBody] UpdateAgentResource resource)
    {
        // Get existing agent first
        var existingAgent = await _domainFacade.GetAgentById(id);
        if (existingAgent == null)
        {
            return NotFound($"Agent with ID {id} not found");
        }

        // Map update to domain object
        var agentToUpdate = AgentMapper.ToDomain(resource, existingAgent);
        var updatedAgent = await _domainFacade.UpdateAgent(agentToUpdate);

        var response = AgentMapper.ToResource(updatedAgent);
        
        return Ok(response);
    }

    /// <summary>
    /// Deletes an agent
    /// </summary>
    /// <param name="id">The ID of the agent</param>
    /// <returns>No content on success</returns>
    /// <response code="204">If the agent was deleted</response>
    /// <response code="404">If the agent is not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _domainFacade.DeleteAgent(id);
        if (!deleted)
        {
            return NotFound($"Agent with ID {id} not found");
        }

        return NoContent();
    }

    /// <summary>
    /// Selects a voice for an agent
    /// </summary>
    /// <param name="id">The ID of the agent</param>
    /// <param name="request">The voice selection details</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Voice selected successfully</response>
    /// <response code="404">If the agent is not found</response>
    [HttpPost("{id}/voice/select")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> SelectVoice(Guid id, [FromBody] SelectAgentVoiceRequest request)
    {
        _logger.LogInformation("SelectVoice called for agent {AgentId} with VoiceId={VoiceId}, VoiceName={VoiceName}", 
            id, request.VoiceId, request.VoiceName);
        
        var agent = await _domainFacade.GetAgentById(id);
        if (agent == null)
        {
            return NotFound($"Agent with ID {id} not found");
        }

        await _domainFacade.SelectAgentVoiceAsync(
            id,
            request.VoiceProvider,
            request.VoiceType,
            request.VoiceId,
            request.VoiceName);

        // Verify the update
        var updatedAgent = await _domainFacade.GetAgentById(id);
        _logger.LogInformation("After SelectVoice for agent {AgentId}: ElevenlabsVoiceId={VoiceId}, VoiceName={VoiceName}", 
            id, updatedAgent?.ElevenlabsVoiceId, updatedAgent?.VoiceName);

        return NoContent();
    }

    /// <summary>
    /// Generates TTS audio for an agent using their configured voice.
    /// Used by interview pages to speak questions to applicants.
    /// </summary>
    /// <param name="id">The ID of the agent</param>
    /// <param name="request">The text to convert to speech</param>
    /// <returns>Streaming audio/mpeg</returns>
    /// <response code="200">Returns the audio stream</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the agent is not found</response>
    /// <response code="503">If TTS service is unavailable</response>
    [HttpPost("{id}/voice/test")]
    [AllowAnonymous] // Public endpoint for interview pages
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(503)]
    public async Task TestVoice(Guid id, [FromBody] TestVoiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Text))
        {
            Response.StatusCode = 400;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = "Text is required." });
            return;
        }

        var agent = await _domainFacade.GetAgentById(id);
        if (agent == null)
        {
            Response.StatusCode = 404;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = $"Agent with ID {id} not found." });
            return;
        }

        // Use agent's voice or fall back to default ElevenLabs voice
        var voiceId = agent.ElevenlabsVoiceId;
        if (string.IsNullOrWhiteSpace(voiceId))
        {
            // Default to "Rachel" voice (ElevenLabs default)
            voiceId = "21m00Tcm4TlvDq8ikWAM";
            _logger.LogWarning("Agent {AgentId} has no voice configured, using default Rachel voice", id);
        }

        Response.ContentType = "audio/mpeg";
        Response.Headers.Append("Cache-Control", "no-cache");

        var bytes = await _domainFacade.PreviewVoiceAsync(voiceId, request.Text, HttpContext.RequestAborted);
        if (bytes != null && bytes.Length > 0)
        {
            await Response.Body.WriteAsync(bytes, HttpContext.RequestAborted);
        }
    }

    /// <summary>
    /// Streams TTS audio for an agent using their configured voice.
    /// Audio chunks are sent as they are generated for low-latency playback.
    /// Used by interview pages to speak questions with minimal delay.
    /// </summary>
    /// <param name="id">The ID of the agent</param>
    /// <param name="request">The text to convert to speech</param>
    /// <returns>Streaming audio/mpeg with chunked transfer encoding</returns>
    /// <response code="200">Returns the streaming audio</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the agent is not found</response>
    /// <response code="503">If TTS service is unavailable</response>
    [HttpPost("{id}/voice/stream")]
    [AllowAnonymous] // Public endpoint for interview pages
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(503)]
    public async Task StreamVoice(Guid id, [FromBody] TestVoiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Text))
        {
            Response.StatusCode = 400;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = "Text is required." });
            return;
        }

        // Try to get voiceId from cache first
        string voiceId;
        var cacheKey = $"agent_voice_{id}";
        
        if (_cache.TryGetValue<string>(cacheKey, out var cachedVoiceId) && !string.IsNullOrWhiteSpace(cachedVoiceId))
        {
            voiceId = cachedVoiceId;
        }
        else
        {
            // Cache miss - fetch agent from database
            var agent = await _domainFacade.GetAgentById(id);
            if (agent == null)
            {
                Response.StatusCode = 404;
                Response.ContentType = "application/json";
                await Response.WriteAsJsonAsync(new { error = $"Agent with ID {id} not found." });
                return;
            }

            // Use agent's voice or fall back to default ElevenLabs voice
            voiceId = agent.ElevenlabsVoiceId ?? "21m00Tcm4TlvDq8ikWAM";
            if (string.IsNullOrWhiteSpace(voiceId))
            {
                // Default to "Rachel" voice (ElevenLabs default)
                voiceId = "21m00Tcm4TlvDq8ikWAM";
            }

            // Cache the voiceId for 10 minutes
            _cache.Set(cacheKey, voiceId, TimeSpan.FromMinutes(10));
        }

        // Set response headers before streaming starts
        // Do NOT set Transfer-Encoding: chunked manually. Kestrel adds it automatically
        // when streaming without Content-Length and applies proper chunk framing.
        // Setting it ourselves causes raw bytes to be sent without framing, so Node's
        // fetch fails with "Invalid character in chunk size" when proxying.
        Response.ContentType = "audio/mpeg";
        Response.Headers.Append("Cache-Control", "no-cache");
        
        // Ensure headers are sent immediately
        await Response.Body.FlushAsync(HttpContext.RequestAborted);

        await foreach (var chunk in _domainFacade.StreamVoiceAsync(voiceId, request.Text, HttpContext.RequestAborted))
        {
            await Response.Body.WriteAsync(chunk, HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);
        }
    }
}

/// <summary>
/// Request for POST /api/v1/Agent/{id}/voice/test
/// </summary>
public class TestVoiceRequest
{
    /// <summary>
    /// The text to convert to speech
    /// </summary>
    public string? Text { get; set; }
}
