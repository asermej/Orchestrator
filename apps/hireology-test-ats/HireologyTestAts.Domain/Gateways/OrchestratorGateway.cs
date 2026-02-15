using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace HireologyTestAts.Domain;

internal sealed class OrchestratorGateway : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private bool _disposed;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public OrchestratorGateway(string baseUrl, string apiKey)
    {
        _http = new HttpClient();
        _baseUrl = baseUrl.TrimEnd('/');
        _apiKey = apiKey;
    }

    // --- Job Methods ---

    public async Task<bool> SyncJobAsync(Job job)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return false;

        var payload = new
        {
            externalJobId = job.ExternalJobId,
            title = job.Title,
            description = job.Description,
            location = job.Location
        };
        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/v1/ats/jobs")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-API-Key", _apiKey);

        var response = await _http.SendAsync(request).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteJobAsync(string externalJobId)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return false;

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/api/v1/ats/jobs/{Uri.EscapeDataString(externalJobId)}");
        request.Headers.Add("X-API-Key", _apiKey);

        var response = await _http.SendAsync(request).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    // --- Agent Methods ---

    /// <summary>
    /// Lists available interview agents from Orchestrator for this organization
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorAgent>> GetAgentsAsync()
    {
        if (string.IsNullOrEmpty(_apiKey))
            return Array.Empty<OrchestratorAgent>();

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/v1/ats/agents");
        request.Headers.Add("X-API-Key", _apiKey);

        var response = await _http.SendAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return Array.Empty<OrchestratorAgent>();

        var agents = await response.Content.ReadFromJsonAsync<List<OrchestratorAgent>>(JsonOptions).ConfigureAwait(false);
        return agents ?? new List<OrchestratorAgent>();
    }

    // --- Interview Configuration Methods ---

    /// <summary>
    /// Lists available interview configurations from Orchestrator for this organization.
    /// Each configuration bundles an agent + questions + scoring rubric.
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorInterviewConfiguration>> GetConfigurationsAsync()
    {
        if (string.IsNullOrEmpty(_apiKey))
            return Array.Empty<OrchestratorInterviewConfiguration>();

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/v1/ats/configurations");
        request.Headers.Add("X-API-Key", _apiKey);

        var response = await _http.SendAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return Array.Empty<OrchestratorInterviewConfiguration>();

        var configs = await response.Content.ReadFromJsonAsync<List<OrchestratorInterviewConfiguration>>(JsonOptions).ConfigureAwait(false);
        return configs ?? new List<OrchestratorInterviewConfiguration>();
    }

    // --- Applicant Methods ---

    /// <summary>
    /// Syncs an applicant to Orchestrator (create or update)
    /// </summary>
    public async Task<bool> SyncApplicantAsync(Applicant applicant, string externalJobId)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return false;

        var payload = new
        {
            externalApplicantId = applicant.Id.ToString(),
            externalJobId = externalJobId,
            firstName = applicant.FirstName,
            lastName = applicant.LastName,
            email = applicant.Email,
            phone = applicant.Phone
        };
        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/v1/ats/applicants")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-API-Key", _apiKey);

        var response = await _http.SendAsync(request).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    // --- Interview Methods ---

    /// <summary>
    /// Creates an interview in Orchestrator and returns the interview ID and invite URL.
    /// When interviewConfigurationId is provided, the agent is resolved from the configuration.
    /// </summary>
    public async Task<OrchestratorCreateInterviewResult> CreateInterviewAsync(
        string externalApplicantId, string externalJobId, Guid interviewConfigurationId)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InterviewRequestValidationException("Orchestrator API key is not configured");

        var payload = new
        {
            externalApplicantId = externalApplicantId,
            externalJobId = externalJobId,
            interviewConfigurationId = interviewConfigurationId,
            interviewType = "voice"
        };
        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/v1/ats/interviews")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-API-Key", _apiKey);

        var response = await _http.SendAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new InterviewRequestValidationException($"Failed to create interview in Orchestrator: {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var interviewId = root.GetProperty("interview").GetProperty("id").GetGuid();

        var inviteElement = root.GetProperty("invite");
        var shortCode = inviteElement.GetProperty("shortCode").GetString() ?? string.Empty;

        // Build the invite URL: the Orchestrator Web base URL + /i/{shortCode}
        // The invite resource might include a url field, otherwise we construct it
        string inviteUrl;
        if (inviteElement.TryGetProperty("url", out var urlElement) && urlElement.GetString() is string url)
        {
            inviteUrl = url;
        }
        else
        {
            // Default: assume Orchestrator Web runs on port 3000
            inviteUrl = $"http://localhost:3000/i/{shortCode}";
        }

        return new OrchestratorCreateInterviewResult
        {
            InterviewId = interviewId,
            InviteUrl = inviteUrl,
            ShortCode = shortCode
        };
    }

    /// <summary>
    /// Gets interview details from Orchestrator, including current invite status.
    /// Returns null if the interview is not found.
    /// </summary>
    public async Task<OrchestratorInterviewStatus?> GetInterviewStatusAsync(Guid orchestratorInterviewId)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return null;

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_baseUrl}/api/v1/ats/interviews/{orchestratorInterviewId}");
        request.Headers.Add("X-API-Key", _apiKey);

        var response = await _http.SendAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return null;

        var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var interviewElement = root.GetProperty("interview");
        var interviewStatus = interviewElement.GetProperty("status").GetString() ?? "unknown";
        var inviteStatus = root.GetProperty("inviteStatus").GetString() ?? "none";

        return new OrchestratorInterviewStatus
        {
            InterviewStatus = interviewStatus,
            InviteStatus = inviteStatus,
        };
    }

    /// <summary>
    /// Refreshes the invite for an existing interview (revokes old, creates new).
    /// Returns the new invite URL and short code.
    /// </summary>
    public async Task<OrchestratorCreateInterviewResult> RefreshInviteAsync(Guid orchestratorInterviewId)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InterviewRequestValidationException("Orchestrator API key is not configured");

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_baseUrl}/api/v1/ats/interviews/{orchestratorInterviewId}/refresh-invite");
        request.Headers.Add("X-API-Key", _apiKey);

        var response = await _http.SendAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new InterviewRequestValidationException($"Failed to refresh invite: {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var inviteElement = root.GetProperty("invite");
        var shortCode = inviteElement.GetProperty("shortCode").GetString() ?? string.Empty;

        string inviteUrl;
        if (root.TryGetProperty("inviteUrl", out var urlElement) && urlElement.GetString() is string url)
        {
            inviteUrl = url;
        }
        else
        {
            inviteUrl = $"http://localhost:3000/i/{shortCode}";
        }

        return new OrchestratorCreateInterviewResult
        {
            InterviewId = orchestratorInterviewId,
            InviteUrl = inviteUrl,
            ShortCode = shortCode
        };
    }

    // --- Webhook Configuration ---

    /// <summary>
    /// Sets the webhook URL for this ATS's organization in Orchestrator
    /// </summary>
    public async Task<bool> SetWebhookUrlAsync(string webhookUrl)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return false;

        var payload = new { webhookUrl = webhookUrl };
        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/api/v1/ats/settings/webhook")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-API-Key", _apiKey);

        var response = await _http.SendAsync(request).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    // --- User Methods ---

    public async Task ProvisionUserAsync(string auth0Sub, string? email, string? name)
    {
        if (string.IsNullOrEmpty(auth0Sub)) return;

        var getUrl = $"{_baseUrl}/api/v1/user/by-auth0-sub/{Uri.EscapeDataString(auth0Sub)}";
        var getResponse = await _http.GetAsync(getUrl).ConfigureAwait(false);
        if (getResponse.IsSuccessStatusCode)
            return;

        if (getResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
            return;

        var safeEmail = !string.IsNullOrWhiteSpace(email) ? email!.Trim() : $"{auth0Sub.Replace("|", "-")}@test-ats.local";
        var (firstName, lastName) = SplitName(name);

        var createPayload = new
        {
            Auth0Sub = auth0Sub,
            Email = safeEmail,
            FirstName = firstName,
            LastName = lastName,
            Phone = (string?)null
        };

        var postResponse = await _http.PostAsJsonAsync($"{_baseUrl}/api/v1/user", createPayload, JsonOptions).ConfigureAwait(false);
        postResponse.EnsureSuccessStatusCode();
    }

    private static (string FirstName, string LastName) SplitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return ("", "");
        var parts = name.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? (parts[0], parts[1]) : (parts[0], "");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _http.Dispose();
            _disposed = true;
        }
    }
}
