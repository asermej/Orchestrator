using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for Message management operations with LLM integration
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly DomainFacade _domainFacade;
    private readonly ILogger<MessageController> _logger;

    public MessageController(DomainFacade domainFacade, ILogger<MessageController> logger)
    {
        _domainFacade = domainFacade;
        _logger = logger;
    }

    /// <summary>
    /// Sends a user message and receives an AI-generated response
    /// This is the main endpoint for chat interactions with LLM integration
    /// </summary>
    /// <param name="resource">The user's message</param>
    /// <returns>The AI assistant's response</returns>
    /// <response code="200">Returns the AI-generated response</response>
    /// <response code="400">If the message is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("send")]
    [ProducesResponseType(typeof(MessageResource), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<MessageResource>> SendMessage([FromBody] SendMessageResource resource)
    {
        var userMessage = MessageMapper.ToDomain(resource);
        var aiResponse = await _domainFacade.CreateUserMessageAndGetAIResponse(userMessage);
        var response = MessageMapper.ToResource(aiResponse);
        
        return Ok(response);
    }

    /// <summary>
    /// Creates a new message directly (without LLM processing)
    /// Use this for manual message creation or system messages
    /// </summary>
    /// <param name="resource">The message data</param>
    /// <returns>The created message with its ID</returns>
    /// <response code="201">Returns the newly created message</response>
    /// <response code="400">If the resource is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(MessageResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<MessageResource>> Create([FromBody] CreateMessageResource resource)
    {
        var message = MessageMapper.ToDomain(resource);
        var createdMessage = await _domainFacade.CreateMessage(message);

        var response = MessageMapper.ToResource(createdMessage);
        
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets a message by ID
    /// </summary>
    /// <param name="id">The ID of the message</param>
    /// <returns>The message if found</returns>
    /// <response code="200">Returns the message</response>
    /// <response code="404">If the message is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MessageResource), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<MessageResource>> GetById(Guid id)
    {
        var message = await _domainFacade.GetMessageById(id);
        if (message == null)
        {
            return NotFound($"Message with ID {id} not found");
        }

        var response = MessageMapper.ToResource(message);
        
        return Ok(response);
    }

    /// <summary>
    /// Searches for messages with pagination
    /// Typically used to retrieve all messages in a conversation
    /// </summary>
    /// <param name="request">The search criteria</param>
    /// <returns>A paginated list of messages in chronological order</returns>
    /// <response code="200">Returns the paginated results</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<MessageResource>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<PaginatedResponse<MessageResource>>> Search([FromQuery] SearchMessageRequest request)
    {
        var result = await _domainFacade.SearchMessages(
            request.ChatId, 
            request.Role, 
            request.Content, 
            request.PageNumber, 
            request.PageSize);

        var response = new PaginatedResponse<MessageResource>
        {
            Items = MessageMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Deletes a message
    /// </summary>
    /// <param name="id">The ID of the message</param>
    /// <returns>No content on success</returns>
    /// <response code="204">If the message was deleted</response>
    /// <response code="404">If the message is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _domainFacade.DeleteMessage(id);
        if (!deleted)
        {
            return NotFound($"Message with ID {id} not found");
        }

        return NoContent();
    }

    /// <summary>
    /// Gets audio for a message (TTS replay).
    /// Checks cache first, generates via ElevenLabs if not cached.
    /// Only works for assistant messages.
    /// </summary>
    /// <param name="id">The ID of the message</param>
    /// <returns>Audio file (MP3)</returns>
    /// <response code="200">Returns the audio file</response>
    /// <response code="400">If the message is not an assistant message</response>
    /// <response code="404">If the message is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="503">If ElevenLabs TTS is disabled</response>
    [HttpGet("{id}/audio")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> GetAudio(Guid id)
    {
        _logger.LogInformation("Getting audio for message {MessageId}", id);

        try
        {
            var audioData = await _domainFacade.GetMessageAudioAsync(id);
            
            Response.Headers.Append("Cache-Control", "public, max-age=86400");
            
            return File(audioData, "audio/mpeg");
        }
        catch (MessageNotFoundException ex)
        {
            _logger.LogWarning(ex, "Message not found: {MessageId}", id);
            return NotFound(ex.Message);
        }
        catch (MessageValidationException ex)
        {
            _logger.LogWarning(ex, "Invalid message type for audio: {MessageId}", id);
            return BadRequest(ex.Message);
        }
        catch (ElevenLabsDisabledException)
        {
            _logger.LogWarning("ElevenLabs TTS is disabled");
            return StatusCode(503, new { error = "Voice response is currently disabled" });
        }
        catch (AgentNotFoundException ex)
        {
            _logger.LogWarning(ex, "Agent not found for message: {MessageId}", id);
            return NotFound(ex.Message);
        }
        catch (ChatNotFoundException ex)
        {
            _logger.LogWarning(ex, "Chat not found for message: {MessageId}", id);
            return NotFound(ex.Message);
        }
    }
}

