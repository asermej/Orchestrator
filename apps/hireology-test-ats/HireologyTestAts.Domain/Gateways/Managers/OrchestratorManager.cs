using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace HireologyTestAts.Domain;

/// <summary>
/// Manages external API communication with the Orchestrator integration.
/// Handles HTTP requests, error handling, and mapping between domain and resource models.
/// Each call accepts a per-group API key so the correct Orchestrator group is targeted.
/// </summary>
internal sealed class OrchestratorManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private readonly List<HttpClient> _createdClients = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrchestratorManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// Resolves the effective API key: uses the provided group key if available,
    /// falls back to the global config key for backward compatibility.
    /// </summary>
    private string? ResolveApiKey(string? groupApiKey)
    {
        if (!string.IsNullOrEmpty(groupApiKey))
            return groupApiKey;

        var config = _serviceLocator.CreateConfigurationProvider();
        var fallback = config.GetGatewayApiKey("Orchestrator");
        return string.IsNullOrEmpty(fallback) ? null : fallback;
    }

    private HttpClient CreateHttpClientWithApiKey(string apiKey)
    {
        var config = _serviceLocator.CreateConfigurationProvider();
        var baseUrl = config.GetGatewayBaseUrl("Orchestrator");
        var timeout = config.GetGatewayTimeout("Orchestrator");
        var client = _serviceLocator.CreateHttpClient(baseUrl, timeout);
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        _createdClients.Add(client);
        return client;
    }

    private HttpClient CreateBaseHttpClient()
    {
        var config = _serviceLocator.CreateConfigurationProvider();
        var baseUrl = config.GetGatewayBaseUrl("Orchestrator");
        var timeout = config.GetGatewayTimeout("Orchestrator");
        var client = _serviceLocator.CreateHttpClient(baseUrl, timeout);
        _createdClients.Add(client);
        return client;
    }

    /// <summary>
    /// Syncs a group to Orchestrator (create or update by external group ID).
    /// Uses the group's stored API key if available, falls back to global config, then bootstrap secret.
    /// Returns the sync result including the Orchestrator API key.
    /// </summary>
    public async Task<OrchestratorSyncGroupResult> SyncGroup(Group group)
    {
        try
        {
            var config = _serviceLocator.CreateConfigurationProvider();
            var selfBaseUrl = config.GetSelfBaseUrl();
            var webhookUrl = $"{selfBaseUrl}/api/v1/webhooks/orchestrator";
            var atsApiKey = config.GetOrchestratorApiKey();

            var requestResource = OrchestratorMapper.ToSyncGroupRequest(group, selfBaseUrl, webhookUrl, atsApiKey);
            var jsonContent = JsonSerializer.Serialize(requestResource);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var apiKey = ResolveApiKey(group.OrchestratorApiKey);
            HttpClient client;
            if (!string.IsNullOrEmpty(apiKey))
            {
                client = CreateHttpClientWithApiKey(apiKey);
            }
            else
            {
                var bootstrapSecret = config.GetOrchestratorBootstrapSecret();
                if (string.IsNullOrEmpty(bootstrapSecret))
                    throw new OrchestratorConnectionException(
                        "Neither Orchestrator API key nor bootstrap secret is configured. Cannot sync group.");

                client = CreateBaseHttpClient();
                client.DefaultRequestHeaders.Add("X-Bootstrap-Secret", bootstrapSecret);
            }

            var response = await client.PostAsync("/api/v1/ats/groups", content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OrchestratorApiException(
                    $"Orchestrator API returned error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseResource = JsonSerializer.Deserialize<OrchestratorSyncGroupResponse>(
                responseContent, JsonOptions);

            if (responseResource == null)
            {
                throw new OrchestratorApiException("Failed to deserialize Orchestrator group sync response");
            }

            return OrchestratorMapper.ToSyncGroupResult(responseResource);
        }
        catch (HttpRequestException ex)
        {
            throw new OrchestratorConnectionException($"Failed to connect to Orchestrator API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrchestratorConnectionException($"Orchestrator API request timed out: {ex.Message}", ex);
        }
        catch (OrchestratorApiException)
        {
            throw;
        }
        catch (OrchestratorConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OrchestratorApiException($"Unexpected error calling Orchestrator API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Syncs a job to Orchestrator (create or update).
    /// Silently skips if no API key is available.
    /// </summary>
    public async Task SyncJob(Job job, string? groupApiKey)
    {
        var apiKey = ResolveApiKey(groupApiKey);
        if (string.IsNullOrEmpty(apiKey)) return;

        try
        {
            var client = CreateHttpClientWithApiKey(apiKey);
            var requestResource = OrchestratorMapper.ToSyncJobRequest(job);
            var jsonContent = JsonSerializer.Serialize(requestResource);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/v1/ats/jobs", content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OrchestratorApiException(
                    $"Orchestrator API returned error: {response.StatusCode} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new OrchestratorConnectionException($"Failed to connect to Orchestrator API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrchestratorConnectionException($"Orchestrator API request timed out: {ex.Message}", ex);
        }
        catch (OrchestratorApiException)
        {
            throw;
        }
        catch (OrchestratorConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OrchestratorApiException($"Unexpected error calling Orchestrator API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes a job from Orchestrator by external job ID.
    /// Silently skips if no API key is available.
    /// </summary>
    public async Task DeleteJob(string externalJobId, string? groupApiKey)
    {
        var apiKey = ResolveApiKey(groupApiKey);
        if (string.IsNullOrEmpty(apiKey)) return;

        try
        {
            var client = CreateHttpClientWithApiKey(apiKey);
            var response = await client.DeleteAsync(
                $"/api/v1/ats/jobs/{Uri.EscapeDataString(externalJobId)}").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OrchestratorApiException(
                    $"Orchestrator API returned error: {response.StatusCode} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new OrchestratorConnectionException($"Failed to connect to Orchestrator API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrchestratorConnectionException($"Orchestrator API request timed out: {ex.Message}", ex);
        }
        catch (OrchestratorApiException)
        {
            throw;
        }
        catch (OrchestratorConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OrchestratorApiException($"Unexpected error calling Orchestrator API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Lists available interview agents from Orchestrator.
    /// Returns an empty list if no API key is available.
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorAgent>> GetAgents(string? groupApiKey)
    {
        var apiKey = ResolveApiKey(groupApiKey);
        if (string.IsNullOrEmpty(apiKey)) return Array.Empty<OrchestratorAgent>();

        try
        {
            var client = CreateHttpClientWithApiKey(apiKey);
            var response = await client.GetAsync("/api/v1/ats/agents").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OrchestratorApiException(
                    $"Orchestrator API returned error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseResource = JsonSerializer.Deserialize<List<OrchestratorAgentResponse>>(
                responseContent, JsonOptions);

            return OrchestratorMapper.ToAgents(responseResource);
        }
        catch (HttpRequestException ex)
        {
            throw new OrchestratorConnectionException($"Failed to connect to Orchestrator API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrchestratorConnectionException($"Orchestrator API request timed out: {ex.Message}", ex);
        }
        catch (OrchestratorApiException)
        {
            throw;
        }
        catch (OrchestratorConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OrchestratorApiException($"Unexpected error calling Orchestrator API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Lists available interview guides from Orchestrator.
    /// Returns an empty list if no API key is available.
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorInterviewGuide>> GetInterviewGuides(string? groupApiKey)
    {
        var apiKey = ResolveApiKey(groupApiKey);
        if (string.IsNullOrEmpty(apiKey)) return Array.Empty<OrchestratorInterviewGuide>();

        try
        {
            var client = CreateHttpClientWithApiKey(apiKey);
            var response = await client.GetAsync("/api/v1/ats/interview-guides").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OrchestratorApiException(
                    $"Orchestrator API returned error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseResource = JsonSerializer.Deserialize<List<OrchestratorInterviewGuideResponse>>(
                responseContent, JsonOptions);

            return OrchestratorMapper.ToInterviewGuides(responseResource);
        }
        catch (HttpRequestException ex)
        {
            throw new OrchestratorConnectionException($"Failed to connect to Orchestrator API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrchestratorConnectionException($"Orchestrator API request timed out: {ex.Message}", ex);
        }
        catch (OrchestratorApiException)
        {
            throw;
        }
        catch (OrchestratorConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OrchestratorApiException($"Unexpected error calling Orchestrator API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Lists available interview configurations from Orchestrator.
    /// Returns an empty list if no API key is available.
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorInterviewConfiguration>> GetConfigurations(string? groupApiKey)
    {
        var apiKey = ResolveApiKey(groupApiKey);
        if (string.IsNullOrEmpty(apiKey)) return Array.Empty<OrchestratorInterviewConfiguration>();

        try
        {
            var client = CreateHttpClientWithApiKey(apiKey);
            var response = await client.GetAsync("/api/v1/ats/configurations").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OrchestratorApiException(
                    $"Orchestrator API returned error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseResource = JsonSerializer.Deserialize<List<OrchestratorInterviewConfigurationResponse>>(
                responseContent, JsonOptions);

            return OrchestratorMapper.ToConfigurations(responseResource);
        }
        catch (HttpRequestException ex)
        {
            throw new OrchestratorConnectionException($"Failed to connect to Orchestrator API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrchestratorConnectionException($"Orchestrator API request timed out: {ex.Message}", ex);
        }
        catch (OrchestratorApiException)
        {
            throw;
        }
        catch (OrchestratorConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OrchestratorApiException($"Unexpected error calling Orchestrator API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Syncs an applicant to Orchestrator (create or update).
    /// Silently skips if no API key is available.
    /// </summary>
    public async Task SyncApplicant(Applicant applicant, string externalJobId, string? groupApiKey)
    {
        var apiKey = ResolveApiKey(groupApiKey);
        if (string.IsNullOrEmpty(apiKey)) return;

        try
        {
            var client = CreateHttpClientWithApiKey(apiKey);
            var requestResource = OrchestratorMapper.ToSyncApplicantRequest(applicant, externalJobId);
            var jsonContent = JsonSerializer.Serialize(requestResource);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/v1/ats/applicants", content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OrchestratorApiException(
                    $"Orchestrator API returned error: {response.StatusCode} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new OrchestratorConnectionException($"Failed to connect to Orchestrator API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrchestratorConnectionException($"Orchestrator API request timed out: {ex.Message}", ex);
        }
        catch (OrchestratorApiException)
        {
            throw;
        }
        catch (OrchestratorConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OrchestratorApiException($"Unexpected error calling Orchestrator API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates an interview in Orchestrator and returns the interview ID and invite URL.
    /// Throws OrchestratorConnectionException if no API key is available.
    /// </summary>
    public async Task<OrchestratorCreateInterviewResult> CreateInterview(
        string externalApplicantId, string externalJobId, Guid agentId, Guid interviewGuideId, string? groupApiKey)
    {
        var apiKey = ResolveApiKey(groupApiKey);
        if (string.IsNullOrEmpty(apiKey))
            throw new OrchestratorConnectionException("Orchestrator API key is not configured. Cannot create interview.");

        try
        {
            var client = CreateHttpClientWithApiKey(apiKey);
            var requestResource = OrchestratorMapper.ToCreateInterviewRequest(
                externalApplicantId, externalJobId, agentId, interviewGuideId);
            var jsonContent = JsonSerializer.Serialize(requestResource);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/v1/ats/interviews", content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OrchestratorApiException(
                    $"Orchestrator API returned error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseResource = JsonSerializer.Deserialize<OrchestratorCreateInterviewResponse>(
                responseContent, JsonOptions);

            if (responseResource == null)
            {
                throw new OrchestratorApiException("Failed to deserialize Orchestrator API response");
            }

            return OrchestratorMapper.ToCreateInterviewResult(responseResource);
        }
        catch (HttpRequestException ex)
        {
            throw new OrchestratorConnectionException($"Failed to connect to Orchestrator API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrchestratorConnectionException($"Orchestrator API request timed out: {ex.Message}", ex);
        }
        catch (OrchestratorApiException)
        {
            throw;
        }
        catch (OrchestratorConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OrchestratorApiException($"Unexpected error calling Orchestrator API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets interview status from Orchestrator, including current invite status.
    /// Returns null if the interview is not found or if no API key is available.
    /// </summary>
    public async Task<OrchestratorInterviewStatus?> GetInterviewStatus(Guid orchestratorInterviewId, string? groupApiKey)
    {
        var apiKey = ResolveApiKey(groupApiKey);
        if (string.IsNullOrEmpty(apiKey)) return null;

        try
        {
            var client = CreateHttpClientWithApiKey(apiKey);
            var response = await client.GetAsync(
                $"/api/v1/ats/interviews/{orchestratorInterviewId}").ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OrchestratorApiException(
                    $"Orchestrator API returned error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseResource = JsonSerializer.Deserialize<OrchestratorInterviewStatusResponse>(
                responseContent, JsonOptions);

            if (responseResource == null)
            {
                throw new OrchestratorApiException("Failed to deserialize Orchestrator API response");
            }

            return OrchestratorMapper.ToInterviewStatus(responseResource);
        }
        catch (HttpRequestException ex)
        {
            throw new OrchestratorConnectionException($"Failed to connect to Orchestrator API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrchestratorConnectionException($"Orchestrator API request timed out: {ex.Message}", ex);
        }
        catch (OrchestratorApiException)
        {
            throw;
        }
        catch (OrchestratorConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OrchestratorApiException($"Unexpected error calling Orchestrator API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Refreshes the invite for an existing interview (revokes old, creates new).
    /// Throws OrchestratorConnectionException if no API key is available.
    /// </summary>
    public async Task<OrchestratorCreateInterviewResult> RefreshInvite(Guid orchestratorInterviewId, string? groupApiKey)
    {
        var apiKey = ResolveApiKey(groupApiKey);
        if (string.IsNullOrEmpty(apiKey))
            throw new OrchestratorConnectionException("Orchestrator API key is not configured. Cannot refresh invite.");

        try
        {
            var client = CreateHttpClientWithApiKey(apiKey);
            var response = await client.PostAsync(
                $"/api/v1/ats/interviews/{orchestratorInterviewId}/refresh-invite", null).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OrchestratorApiException(
                    $"Orchestrator API returned error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseResource = JsonSerializer.Deserialize<OrchestratorRefreshInviteResponse>(
                responseContent, JsonOptions);

            if (responseResource == null)
            {
                throw new OrchestratorApiException("Failed to deserialize Orchestrator API response");
            }

            return OrchestratorMapper.ToRefreshInviteResult(orchestratorInterviewId, responseResource);
        }
        catch (HttpRequestException ex)
        {
            throw new OrchestratorConnectionException($"Failed to connect to Orchestrator API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrchestratorConnectionException($"Orchestrator API request timed out: {ex.Message}", ex);
        }
        catch (OrchestratorApiException)
        {
            throw;
        }
        catch (OrchestratorConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OrchestratorApiException($"Unexpected error calling Orchestrator API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Provisions a user in Orchestrator. Checks if user exists first, creates if not found.
    /// Silently skips if no API key is available.
    /// </summary>
    public async Task ProvisionUser(string auth0Sub, string? email, string? name, string? groupApiKey)
    {
        if (string.IsNullOrEmpty(auth0Sub)) return;
        var apiKey = ResolveApiKey(groupApiKey);
        if (string.IsNullOrEmpty(apiKey)) return;

        try
        {
            var client = CreateHttpClientWithApiKey(apiKey);

            // Check if user already exists
            var getResponse = await client.GetAsync(
                $"/api/v1/user/by-auth0-sub/{Uri.EscapeDataString(auth0Sub)}").ConfigureAwait(false);

            if (getResponse.IsSuccessStatusCode)
                return; // User already exists

            if (getResponse.StatusCode != HttpStatusCode.NotFound)
            {
                var errorContent = await getResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OrchestratorApiException(
                    $"Orchestrator API returned error: {getResponse.StatusCode} - {errorContent}");
            }

            // User not found, create them
            var requestResource = OrchestratorMapper.ToProvisionUserRequest(auth0Sub, email, name);
            var jsonContent = JsonSerializer.Serialize(requestResource);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var postResponse = await client.PostAsync("/api/v1/user", content).ConfigureAwait(false);

            if (!postResponse.IsSuccessStatusCode)
            {
                var errorContent = await postResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OrchestratorApiException(
                    $"Orchestrator API returned error: {postResponse.StatusCode} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new OrchestratorConnectionException($"Failed to connect to Orchestrator API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrchestratorConnectionException($"Orchestrator API request timed out: {ex.Message}", ex);
        }
        catch (OrchestratorApiException)
        {
            throw;
        }
        catch (OrchestratorConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OrchestratorApiException($"Unexpected error calling Orchestrator API: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        foreach (var client in _createdClients)
        {
            client.Dispose();
        }
        _createdClients.Clear();
    }
}
