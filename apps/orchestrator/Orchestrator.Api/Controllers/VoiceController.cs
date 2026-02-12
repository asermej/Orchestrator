using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for voice selection and preview.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class VoiceController : ControllerBase
{
    private readonly DomainFacade _domainFacade;
    private readonly ILogger<VoiceController> _logger;

    public VoiceController(DomainFacade domainFacade, ILogger<VoiceController> logger)
    {
        _domainFacade = domainFacade;
        _logger = logger;
    }

    /// <summary>
    /// Returns available ElevenLabs voices (prebuilt; fake mode returns deterministic list).
    /// </summary>
    /// <response code="200">Available voices</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet("elevenlabs")]
    [ProducesResponseType(typeof(AvailableVoicesResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AvailableVoicesResponse>> GetElevenLabsVoices()
    {
        var voices = await _domainFacade.GetAvailableVoicesAsync(HttpContext.RequestAborted);
        var response = VoiceMapper.ToAvailableVoicesResponse(voices);
        return Ok(response);
    }

    /// <summary>
    /// Returns curated stock voices from the database (for Choose a voice).
    /// </summary>
    /// <response code="200">Curated stock voices</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet("elevenlabs/stock")]
    [ProducesResponseType(typeof(AvailableVoicesResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AvailableVoicesResponse>> GetStockVoices()
    {
        var stockVoices = await _domainFacade.GetStockVoicesAsync(HttpContext.RequestAborted);
        var list = VoiceMapper.ToStockVoicesResponse(stockVoices);
        var response = new AvailableVoicesResponse
        {
            CuratedPrebuiltVoices = list,
            UserVoices = new List<VoiceResource>()
        };
        return Ok(response);
    }

    /// <summary>
    /// Previews a voice by generating a short audio sample.
    /// </summary>
    /// <response code="200">Streaming audio/mpeg</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost("preview")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(503)]
    public async Task Preview([FromBody] PreviewVoiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VoiceId))
        {
            Response.StatusCode = 400;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = "VoiceId is required." });
            return;
        }

        var text = string.IsNullOrWhiteSpace(request.Text) ? "Hey — I'm your Surrova agent voice." : request.Text;

        Response.ContentType = "audio/mpeg";
        Response.Headers.Append("Cache-Control", "no-cache");

        var bytes = await _domainFacade.PreviewVoiceAsync(request.VoiceId, text, HttpContext.RequestAborted);
        if (bytes != null && bytes.Length > 0)
        {
            await Response.Body.WriteAsync(bytes, HttpContext.RequestAborted);
        }
    }
}
