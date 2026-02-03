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
        Guid organizationId,
        string eventType,
        Interview interview,
        InterviewResult? result = null)
    {
        try
        {
            // Get the organization to find the webhook URL
            var organization = await _dataFacade.GetOrganizationByIdAsync(organizationId);
            if (organization == null || string.IsNullOrEmpty(organization.WebhookUrl))
            {
                _logger?.LogWarning("No webhook URL configured for organization {OrganizationId}", organizationId);
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
            return await SendWebhookAsync(organization.WebhookUrl, organization.ApiKey, eventType, payloadJson);
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

    /// <summary>
    /// Send a webhook request
    /// </summary>
    private async Task<bool> SendWebhookAsync(string url, string? secret, string eventType, string payload)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            // Add webhook headers
            request.Headers.Add("X-Webhook-Event", eventType);
            request.Headers.Add("X-Webhook-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            // Add signature if secret is configured
            if (!string.IsNullOrEmpty(secret))
            {
                var signature = ComputeSignature(payload, secret);
                request.Headers.Add("X-Webhook-Signature", signature);
            }

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger?.LogInformation("Webhook delivered successfully to {Url} for event {EventType}", url, eventType);
                return true;
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger?.LogWarning(
                    "Webhook delivery failed to {Url}: {StatusCode} - {ResponseBody}",
                    url,
                    response.StatusCode,
                    responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception while sending webhook to {Url}", url);
            return false;
        }
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
