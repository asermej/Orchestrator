using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
[Produces("application/json")]
public class WebhookController : ControllerBase
{
    private readonly DomainFacade _domainFacade;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(DomainFacade domainFacade, ILogger<WebhookController> logger)
    {
        _domainFacade = domainFacade;
        _logger = logger;
    }

    /// <summary>
    /// Receives webhook notifications from Orchestrator (e.g., interview.completed, interview.started)
    /// </summary>
    [HttpPost("orchestrator")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> ReceiveWebhook()
    {
        try
        {
            var eventType = Request.Headers["X-Webhook-Event"].FirstOrDefault();
            _logger.LogInformation("Received webhook from Orchestrator: event={EventType}", eventType);

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(body))
            {
                return BadRequest("Empty webhook body");
            }

            var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault();
            var timestamp = Request.Headers["X-Webhook-Timestamp"].FirstOrDefault();

            if (!_domainFacade.VerifyWebhookSignature(body, signature, timestamp))
            {
                _logger.LogWarning("Webhook signature verification failed for event {EventType}", eventType);
                return Unauthorized("Invalid webhook signature");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (!root.TryGetProperty("interview", out var interviewElement))
            {
                _logger.LogWarning("Webhook payload missing 'interview' property");
                return BadRequest("Missing interview data");
            }

            var interviewId = interviewElement.GetProperty("id").GetGuid();
            var status = interviewElement.GetProperty("status").GetString() ?? "completed";

            var mappedStatus = status switch
            {
                "completed" => InterviewRequestStatus.Completed,
                "in_progress" => InterviewRequestStatus.InProgress,
                _ => status
            };

            int? score = null;
            string? summary = null;
            string? recommendation = null;
            string? strengths = null;
            string? areasForImprovement = null;

            if (root.TryGetProperty("result", out var resultElement) && resultElement.ValueKind != JsonValueKind.Null)
            {
                if (resultElement.TryGetProperty("score", out var scoreEl) && scoreEl.ValueKind == JsonValueKind.Number)
                    score = scoreEl.GetInt32();

                if (resultElement.TryGetProperty("summary", out var summaryEl) && summaryEl.ValueKind == JsonValueKind.String)
                    summary = summaryEl.GetString();

                if (resultElement.TryGetProperty("recommendation", out var recEl) && recEl.ValueKind == JsonValueKind.String)
                    recommendation = recEl.GetString();

                if (resultElement.TryGetProperty("strengths", out var strEl) && strEl.ValueKind == JsonValueKind.String)
                    strengths = strEl.GetString();

                if (resultElement.TryGetProperty("areasForImprovement", out var impEl) && impEl.ValueKind == JsonValueKind.String)
                    areasForImprovement = impEl.GetString();
            }

            try
            {
                await _domainFacade.UpdateInterviewRequestFromWebhook(
                    interviewId, mappedStatus, score, summary, recommendation, strengths, areasForImprovement);

                _logger.LogInformation(
                    "Updated interview request from webhook: orchestratorInterviewId={InterviewId}, status={Status}, score={Score}",
                    interviewId, mappedStatus, score);
            }
            catch (InterviewRequestNotFoundException)
            {
                _logger.LogWarning(
                    "No local interview request found for orchestrator interview ID {InterviewId}. Webhook ignored.",
                    interviewId);
            }

            return Ok(new { received = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse webhook payload");
            return BadRequest("Invalid JSON payload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return Ok(new { received = true, error = "Processing error" });
        }
    }
}
