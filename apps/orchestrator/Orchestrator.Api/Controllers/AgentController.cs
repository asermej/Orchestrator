using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;
using Orchestrator.Api.Middleware;

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

    private UserContext? GetUserContext()
        => HttpContext.Items.TryGetValue("UserContext", out var ctx) ? ctx as UserContext : null;

    /// <summary>
    /// Reads the X-Organization-Id header to determine the currently selected organization.
    /// </summary>
    private Guid? GetSelectedOrganizationId()
    {
        if (HttpContext.Request.Headers.TryGetValue("X-Organization-Id", out var orgIdHeader)
            && Guid.TryParse(orgIdHeader.FirstOrDefault(), out var orgId))
        {
            return orgId;
        }
        return null;
    }

    /// <summary>
    /// Computes the ancestor organization IDs for the given org by walking up the parent chain.
    /// </summary>
    private List<Guid> GetAncestorOrgIds(Guid currentOrgId, UserContext userContext)
    {
        var ancestors = new List<Guid>();
        var orgMap = userContext.AccessibleOrganizations.ToDictionary(o => o.Id);
        var currentId = currentOrgId;
        while (orgMap.TryGetValue(currentId, out var org) && org.ParentOrganizationId.HasValue)
        {
            ancestors.Add(org.ParentOrganizationId.Value);
            currentId = org.ParentOrganizationId.Value;
        }
        return ancestors;
    }

    /// <summary>
    /// Builds a lookup of organization ID to name from the user context.
    /// </summary>
    private Dictionary<Guid, string> BuildOrgNameLookup(UserContext userContext)
    {
        return userContext.AccessibleOrganizations.ToDictionary(o => o.Id, o => o.Name);
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
        // Use the internal group ID resolved by UserContextMiddleware (external → internal),
        // falling back to the resource value or a default group
        var userContext = GetUserContext();
        var groupId = userContext?.GroupId ?? resource.GroupId ?? await GetOrCreateDefaultGroupAsync();

        // Set organization ID from header if not explicitly provided in the request body
        if (!resource.OrganizationId.HasValue)
        {
            resource.OrganizationId = GetSelectedOrganizationId();
        }
        
        var agent = AgentMapper.ToDomain(resource, groupId);
        var createdAgent = await _domainFacade.CreateAgent(agent);

        var response = AgentMapper.ToResource(createdAgent);
        
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets or creates a default group for agents when none is specified
    /// </summary>
    private async Task<Guid> GetOrCreateDefaultGroupAsync()
    {
        const string defaultGroupName = "Default Group";
        
        // Search for existing default group
        var existingGroups = await _domainFacade.SearchGroups(defaultGroupName, true, 1, 1);
        if (existingGroups.Items.Any())
        {
            return existingGroups.Items.First().Id;
        }
        
        // Create default group if it doesn't exist
        var defaultGroup = new Group
        {
            Name = defaultGroupName,
            IsActive = true
        };
        var created = await _domainFacade.CreateGroup(defaultGroup);
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
        var userContext = GetUserContext();
        var groupId = request.GroupId ?? userContext?.GroupId;
        var selectedOrgId = GetSelectedOrganizationId();

        // When Source is specified (local/inherited), require an organization to be selected
        if (!string.IsNullOrWhiteSpace(request.Source) && !selectedOrgId.HasValue)
        {
            return BadRequest("An organization must be selected to filter by source.");
        }

        // Handle local/inherited source filtering
        if (!string.IsNullOrWhiteSpace(request.Source) && selectedOrgId.HasValue && groupId.HasValue && userContext != null)
        {
            var orgNameLookup = BuildOrgNameLookup(userContext);

            if (request.Source.Equals("local", StringComparison.OrdinalIgnoreCase))
            {
                var localResult = await _domainFacade.SearchLocalAgents(
                    groupId.Value,
                    selectedOrgId.Value,
                    request.DisplayName,
                    request.SortBy,
                    request.PageNumber,
                    request.PageSize);

                return Ok(new PaginatedResponse<AgentResource>
                {
                    Items = AgentMapper.ToResource(localResult.Items, isInherited: false, orgNameLookup: orgNameLookup),
                    TotalCount = localResult.TotalCount,
                    PageNumber = localResult.PageNumber,
                    PageSize = localResult.PageSize
                });
            }

            if (request.Source.Equals("inherited", StringComparison.OrdinalIgnoreCase))
            {
                var ancestorOrgIds = GetAncestorOrgIds(selectedOrgId.Value, userContext);

                var inheritedResult = await _domainFacade.SearchInheritedAgents(
                    groupId.Value,
                    ancestorOrgIds,
                    request.DisplayName,
                    request.SortBy,
                    request.PageNumber,
                    request.PageSize);

                return Ok(new PaginatedResponse<AgentResource>
                {
                    Items = AgentMapper.ToResource(inheritedResult.Items, isInherited: true, orgNameLookup: orgNameLookup),
                    TotalCount = inheritedResult.TotalCount,
                    PageNumber = inheritedResult.PageNumber,
                    PageSize = inheritedResult.PageSize
                });
            }
        }

        // Legacy behavior: return all agents the user can access
        var orgFilter = (userContext is { IsResolved: true, IsSuperadmin: false, IsGroupAdmin: false })
            ? userContext.AccessibleOrganizationIds
            : null;

        var result = await _domainFacade.SearchAgents(
            groupId,
            request.DisplayName,
            request.CreatedBy,
            request.SortBy,
            request.PageNumber, 
            request.PageSize,
            orgFilter);

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

        // Ownership guard: only the creating org can edit an agent
        var selectedOrgId = GetSelectedOrganizationId();
        if (selectedOrgId.HasValue && existingAgent.OrganizationId.HasValue
            && selectedOrgId.Value != existingAgent.OrganizationId.Value)
        {
            return StatusCode(403, "Cannot edit an agent owned by a different organization.");
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
        // Ownership guard: only the creating org can delete an agent
        var selectedOrgId = GetSelectedOrganizationId();
        var existingAgent = await _domainFacade.GetAgentById(id);
        if (existingAgent != null && selectedOrgId.HasValue && existingAgent.OrganizationId.HasValue
            && selectedOrgId.Value != existingAgent.OrganizationId.Value)
        {
            return StatusCode(403, "Cannot delete an agent owned by a different organization.");
        }

        var deleted = await _domainFacade.DeleteAgent(id);
        if (!deleted)
        {
            return NotFound($"Agent with ID {id} not found");
        }

        return NoContent();
    }

    /// <summary>
    /// Clones an inherited agent into the currently selected organization as a local agent.
    /// </summary>
    /// <param name="id">The ID of the agent to clone</param>
    /// <returns>The newly created cloned agent</returns>
    /// <response code="201">Returns the cloned agent</response>
    /// <response code="400">If no organization is selected</response>
    /// <response code="404">If the source agent is not found</response>
    [HttpPost("{id}/clone")]
    [ProducesResponseType(typeof(AgentResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<AgentResource>> Clone(Guid id)
    {
        var userContext = GetUserContext();
        var selectedOrgId = GetSelectedOrganizationId();
        var groupId = userContext?.GroupId;

        if (!selectedOrgId.HasValue)
        {
            return BadRequest("An organization must be selected to clone an agent.");
        }

        if (!groupId.HasValue)
        {
            return BadRequest("Group context is required.");
        }

        var clonedAgent = await _domainFacade.CloneAgent(id, selectedOrgId.Value, groupId.Value);
        var response = AgentMapper.ToResource(clonedAgent);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
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
