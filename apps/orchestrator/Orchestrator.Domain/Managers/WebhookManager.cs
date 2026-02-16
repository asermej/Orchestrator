using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Orchestrator.Domain;

/// <summary>
/// Manages webhook delivery for ATS integration
/// </summary>
internal class WebhookManager : IDisposable
{
    private readonly DataFacade _dataFacade;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookManager>? _logger;
    private bool _disposed;

    public WebhookManager(DataFacade dataFacade, ILogger<WebhookManager>? logger = null)
    {
        _dataFacade = dataFacade;
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// Send a webhook notification for an interview event
    /// </summary>
    public async Task<bool> SendInterviewWebhookAsync(
        Guid groupId,
        string eventType,
        Interview interview,
        InterviewResult? result = null)
    {
        try
        {
            // Get the group to find the webhook URL
            var group = await _dataFacade.GetGroupByIdAsync(groupId);
            if (group == null || string.IsNullOrEmpty(group.WebhookUrl))
            {
                _logger?.LogWarning("No webhook URL configured for group {GroupId}", groupId);
                return false;
            }

            // Build the webhook payload
            var payload = await BuildInterviewPayloadAsync(eventType, interview, result);
            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // Send the webhook
            return await SendWebhookAsync(group.WebhookUrl, group.ApiKey, eventType, payloadJson);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send webhook for interview {InterviewId}", interview.Id);
            return false;
        }
    }

    /// <summary>
    /// Build the webhook payload for an interview event
    /// </summary>
    private async Task<object> BuildInterviewPayloadAsync(string eventType, Interview interview, InterviewResult? result)
    {
        // Fetch related data for external IDs
        var job = await _dataFacade.GetJobById(interview.JobId);
        var applicant = await _dataFacade.GetApplicantById(interview.ApplicantId);

        var payload = new
        {
            eventType,
            timestamp = DateTime.UtcNow.ToString("O"),
            interview = new
            {
                id = interview.Id,
                externalJobId = job?.ExternalJobId,
                token = interview.Token,
                status = interview.Status,
                startedAt = interview.StartedAt?.ToString("O"),
                completedAt = interview.CompletedAt?.ToString("O"),
                applicant = new
                {
                    id = interview.ApplicantId,
                    externalApplicantId = applicant?.ExternalApplicantId,
                    firstName = applicant?.FirstName,
                    lastName = applicant?.LastName,
                    email = applicant?.Email
                }
            },
            result = result == null ? null : new
            {
                summary = result.Summary,
                strengths = result.Strengths,
                areasForImprovement = result.AreasForImprovement,
                score = result.Score,
                recommendation = result.Recommendation
            }
        };

        return payload;
    }

    private const int MaxRetries = 3;
    private static readonly int[] RetryDelaysMs = { 1_000, 5_000, 25_000 };

    /// <summary>
    /// Send a webhook request with exponential backoff retries
    /// </summary>
    private async Task<bool> SendWebhookAsync(string url, string? secret, string eventType, string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 0)
            {
                var delayMs = RetryDelaysMs[attempt - 1];
                _logger?.LogInformation(
                    "Webhook retry {Attempt}/{MaxRetries} to {Url} in {DelayMs}ms",
                    attempt, MaxRetries, url, delayMs);
                await Task.Delay(delayMs).ConfigureAwait(false);
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("X-Webhook-Event", eventType);
                request.Headers.Add("X-Webhook-Timestamp", timestamp);

                if (!string.IsNullOrEmpty(secret))
                {
                    var signature = ComputeSignature(payload, secret);
                    request.Headers.Add("X-Webhook-Signature", signature);
                }

                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation(
                        "Webhook delivered successfully to {Url} for event {EventType} (attempt {Attempt})",
                        url, eventType, attempt + 1);
                    return true;
                }

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger?.LogWarning(
                    "Webhook delivery failed to {Url}: {StatusCode} - {ResponseBody} (attempt {Attempt}/{Total})",
                    url, response.StatusCode, responseBody, attempt + 1, MaxRetries + 1);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "Exception sending webhook to {Url} (attempt {Attempt}/{Total})",
                    url, attempt + 1, MaxRetries + 1);
            }
        }

        _logger?.LogError(
            "Webhook delivery to {Url} for event {EventType} failed after {Total} attempts",
            url, eventType, MaxRetries + 1);
        return false;
    }

    /// <summary>
    /// Compute HMAC-SHA256 signature for webhook payload verification
    /// </summary>
    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
