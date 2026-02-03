using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for voice conversation operations.
/// Handles streaming audio responses for voice-based chat interactions.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ConversationController : ControllerBase
{
    private readonly DomainFacade _domainFacade;
    private readonly ILogger<ConversationController> _logger;

    public ConversationController(DomainFacade domainFacade, ILogger<ConversationController> logger)
    {
        _domainFacade = domainFacade;
        _logger = logger;
    }

    /// <summary>
    /// Sends a text message and receives a streaming audio response from the agent.
    /// The user's speech is transcribed in the browser before calling this endpoint.
    /// </summary>
    /// <param name="request">Text message and context (chatId, agentId, message)</param>
    /// <returns>Streaming audio/mpeg of agent's spoken response</returns>
    /// <response code="200">Returns streaming audio response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="503">If ElevenLabs TTS is disabled</response>
    [HttpPost("respond/audio")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(503)]
    public async Task GetAudioResponse([FromBody] AudioResponseRequest request)
    {
        _logger.LogInformation("Streaming audio response for chat {ChatId}, agent {AgentId}", 
            request.ChatId, request.AgentId);

        Response.ContentType = "audio/mpeg";
        Response.Headers.Append("Cache-Control", "no-cache");
        // Do NOT set Transfer-Encoding: chunked manually. Kestrel adds it automatically
        // when streaming without Content-Length and applies proper chunk framing.
        // Setting it ourselves causes raw bytes to be sent without framing, so Node's
        // fetch fails with "Invalid character in chunk size" when proxying.

        try
        {
            await foreach (var audioChunk in _domainFacade.StreamAudioResponseAsync(
                request.ChatId, 
                request.AgentId, 
                request.Message, 
                HttpContext.RequestAborted))
            {
                await Response.Body.WriteAsync(audioChunk, HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
            }
        }
        catch (ElevenLabsDisabledException)
        {
            _logger.LogWarning("ElevenLabs TTS is disabled");
            Response.StatusCode = 503;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = "Voice response is currently disabled" });
        }
        catch (AgentNotFoundException ex)
        {
            _logger.LogWarning(ex, "Agent not found: {AgentId}", request.AgentId);
            Response.StatusCode = 404;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (ElevenLabsApiException ex)
        {
            _logger.LogWarning(ex, "ElevenLabs API error during audio stream");
            if (!Response.HasStarted)
            {
                Response.StatusCode = 402; // Payment Required / quota exceeded
                Response.ContentType = "application/json";
                var message = ex.Message.Contains("quota", StringComparison.OrdinalIgnoreCase)
                    ? "Voice quota exceeded. Please try again later or upgrade your plan."
                    : "Voice service error. Please try again.";
                await Response.WriteAsJsonAsync(new { error = message });
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or request was aborted; not an error
            _logger.LogInformation("Audio stream canceled (client disconnected or request aborted)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming audio response");
            if (!Response.HasStarted)
            {
                Response.StatusCode = 500;
                Response.ContentType = "application/json";
                await Response.WriteAsJsonAsync(new { error = "An error occurred while generating the audio response" });
            }
            // If response has started, we can't change the status code
            // The client will receive a truncated audio stream
        }
    }

    /// <summary>
    /// Checks if voice conversation is enabled.
    /// </summary>
    /// <returns>Status of voice conversation feature</returns>
    /// <response code="200">Returns the enabled status</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(VoiceStatusResponse), 200)]
    [ProducesResponseType(401)]
    public ActionResult<VoiceStatusResponse> GetVoiceStatus()
    {
        var isEnabled = _domainFacade.IsVoiceEnabled();
        return Ok(new VoiceStatusResponse { Enabled = isEnabled });
    }
}

/// <summary>
/// Response model for voice status endpoint
/// </summary>
public class VoiceStatusResponse
{
    public bool Enabled { get; set; }
}
