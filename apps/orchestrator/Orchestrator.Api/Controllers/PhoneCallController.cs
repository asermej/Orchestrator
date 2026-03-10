using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Handles inbound Telnyx phone call webhooks.
/// </summary>
[ApiController]
[Route("api/v1/phonecall")]
[AllowAnonymous]
public class PhoneCallController : ControllerBase
{
    private readonly ILogger<PhoneCallController> _logger;

    public PhoneCallController(ILogger<PhoneCallController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Telnyx inbound call webhook. Returns TeXML that instructs Telnyx to open
    /// a bidirectional media stream WebSocket back to this server.
    /// </summary>
    /// <response code="200">TeXML response</response>
    [HttpGet("webhook")]
    [HttpPost("webhook")]
    [Produces("text/xml")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> InboundCall()
    {
        var host = Request.Headers["Host"].ToString();
        var forwardedProto = Request.Headers["X-Forwarded-Proto"].ToString();
        var isSecure = Request.IsHttps
                       || forwardedProto.Equals("https", StringComparison.OrdinalIgnoreCase);
        var scheme = isSecure ? "wss" : "ws";
        var method = Request.Method;
        var callerId = Request.Headers["X-Telnyx-Caller-Id"].ToString();
        var contentType = Request.ContentType;

        // Read body first so we can extract caller info from form data
        string? body = null;
        if (Request.ContentLength > 0)
        {
            Request.EnableBuffering();
            Request.Body.Position = 0;
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
        }

        // Try to extract caller number from form-encoded body (Telnyx sends From=+1...)
        var callerNumber = callerId;
        if (string.IsNullOrEmpty(callerNumber) && !string.IsNullOrEmpty(body))
        {
            var fromMatch = System.Text.RegularExpressions.Regex.Match(body, @"(?:From|CallerId)=(%2B[^&]+|[^&]+)");
            if (fromMatch.Success)
            {
                callerNumber = Uri.UnescapeDataString(fromMatch.Groups[1].Value);
            }
        }

        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("[PHONE][WEBHOOK] ☎️  INBOUND CALL RECEIVED");
        Console.WriteLine($"[PHONE][WEBHOOK]   Method:       {method}");
        Console.WriteLine($"[PHONE][WEBHOOK]   Host:         {host}");
        Console.WriteLine($"[PHONE][WEBHOOK]   Forwarded:    {(string.IsNullOrEmpty(forwardedProto) ? "(none)" : forwardedProto)} → scheme={scheme}");
        Console.WriteLine($"[PHONE][WEBHOOK]   Caller:       {(string.IsNullOrEmpty(callerNumber) ? "(unknown)" : callerNumber)}");
        Console.WriteLine($"[PHONE][WEBHOOK]   Content-Type: {contentType ?? "(none)"}");

        if (!string.IsNullOrWhiteSpace(body))
        {
            Console.WriteLine($"[PHONE][WEBHOOK]   Body:         {body}");
        }

        var streamUrl = $"{scheme}://{host}/api/v1/phonecall/media-stream";
        Console.WriteLine($"[PHONE][WEBHOOK]   Directing media stream to: {streamUrl}");
        Console.WriteLine("════════════════════════════════════════════════════════════");

        var texml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
  <Connect>
    <Stream
      url=""{streamUrl}""
      bidirectionalMode=""rtp""
      bidirectionalCodec=""PCMU""
      bidirectionalSamplingRate=""8000""
    />
  </Connect>
</Response>";

        Console.WriteLine($"[PHONE][WEBHOOK]   Responding with TeXML ({texml.Length} bytes)");

        return Content(texml, "text/xml");
    }

    /// <summary>
    /// Health check for the phone call subsystem.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(200)]
    public IActionResult Health()
    {
        return Ok(new { message = "Telnyx + ElevenLabs phone call server is running" });
    }
}
