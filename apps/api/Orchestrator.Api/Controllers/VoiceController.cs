using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for voice selection, cloning, and preview.
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

    private string? GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
    }

    /// <summary>
    /// Records consent for voice cloning (required before IVC). Returns consentRecordId for use in clone.
    /// </summary>
    /// <response code="200">Consent recorded</response>
    /// <response code="400">Attested must be true</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost("consent")]
    [ProducesResponseType(typeof(RecordConsentResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<RecordConsentResponse>> RecordConsent([FromBody] RecordConsentRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (!request.Attested)
        {
            return BadRequest(new { error = "Consent must be attested." });
        }

        var id = await _domainFacade.RecordConsentAsync(userId, request.PersonaId, request.ConsentTextVersion, request.Attested, HttpContext.RequestAborted);
        return Ok(new RecordConsentResponse { ConsentRecordId = id });
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
    /// Clones a voice from a sample (IVC). Send multipart: file (audio) + form fields personaId, voiceName, consentRecordId, sampleDurationSeconds.
    /// </summary>
    /// <response code="200">Clone result</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost("clone")]
    [ProducesResponseType(typeof(CloneVoiceResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(429)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<ActionResult<CloneVoiceResponse>> Clone([FromForm] Guid personaId, [FromForm] string voiceName, [FromForm] Guid consentRecordId, [FromForm] int sampleDurationSeconds, [FromForm] string? styleLane, IFormFile? file)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "Audio file is required." });
        }

        byte[] bytes;
        using (var stream = file.OpenReadStream())
        using (var ms = new MemoryStream())
        {
            await stream.CopyToAsync(ms, HttpContext.RequestAborted);
            bytes = ms.ToArray();
        }

        string? sampleBlobUrl = null;
        try
        {
            sampleBlobUrl = await _domainFacade.UploadVoiceSampleAsync(bytes, file.FileName, file.ContentType, HttpContext.RequestAborted);
        }
        catch
        {
            _logger.LogWarning("Voice sample blob upload failed; proceeding without blob reference for audit.");
        }

        var result = await _domainFacade.CloneVoiceAsync(userId, personaId, voiceName, sampleBlobUrl, bytes, sampleDurationSeconds, consentRecordId, styleLane, HttpContext.RequestAborted);
        return Ok(VoiceMapper.ToResource(result));
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

        var text = string.IsNullOrWhiteSpace(request.Text) ? "Hey — I'm your Surrova persona voice." : request.Text;

        Response.ContentType = "audio/mpeg";
        Response.Headers.Append("Cache-Control", "no-cache");

        var bytes = await _domainFacade.PreviewVoiceAsync(request.VoiceId, text, HttpContext.RequestAborted);
        if (bytes != null && bytes.Length > 0)
        {
            await Response.Body.WriteAsync(bytes, HttpContext.RequestAborted);
        }
    }
}
