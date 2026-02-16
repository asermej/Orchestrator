using System.Net;
using System.Text.Json;

namespace Orchestrator.Domain;

/// <summary>
/// Manages external API communication with the ATS.
/// Unlike other gateway managers, the ATS base URL and API key come from the Group record,
/// not from global configuration. Each call creates or reuses an HttpClient for the given base URL.
/// </summary>
internal sealed class AtsGatewayManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private readonly Dictionary<string, HttpClient> _httpClients = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AtsGatewayManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    private HttpClient GetHttpClient(string atsBaseUrl, string apiKey)
    {
        if (_httpClients.TryGetValue(atsBaseUrl, out var existing))
            return existing;

        var client = _serviceLocator.CreateHttpClient(atsBaseUrl, 30);
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        _httpClients[atsBaseUrl] = client;
        return client;
    }

    /// <summary>
    /// Gets the user's group/organization access from the ATS.
    /// </summary>
    /// <param name="atsBaseUrl">Base URL of the ATS API (from Group.AtsBaseUrl)</param>
    /// <param name="apiKey">API key for the ATS (from Group.ApiKey)</param>
    /// <param name="auth0Sub">The user's Auth0 sub identifier</param>
    /// <returns>User access information including accessible groups and organizations</returns>
    public async Task<AtsUserAccess> GetUserAccess(string atsBaseUrl, string apiKey, string auth0Sub)
    {
        if (string.IsNullOrEmpty(atsBaseUrl))
            throw new AtsApiException("ATS base URL is not configured for this group");

        try
        {
            var client = GetHttpClient(atsBaseUrl, apiKey);
            var response = await client.GetAsync(
                $"/api/v1/external/user-access?auth0Sub={Uri.EscapeDataString(auth0Sub)}").ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new AtsUserAccess { Auth0Sub = auth0Sub };

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new AtsApiException(
                    $"ATS API returned error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseResource = JsonSerializer.Deserialize<AtsUserAccessResponse>(responseContent, JsonOptions);

            if (responseResource == null)
                throw new AtsApiException("Failed to deserialize ATS user-access response");

            return MapToUserAccess(responseResource);
        }
        catch (HttpRequestException ex)
        {
            throw new AtsConnectionException($"Failed to connect to ATS API at {atsBaseUrl}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new AtsConnectionException($"ATS API request timed out: {ex.Message}", ex);
        }
        catch (AtsApiException)
        {
            throw;
        }
        catch (AtsConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AtsApiException($"Unexpected error calling ATS API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all organizations from the ATS, optionally filtered by group.
    /// </summary>
    public async Task<IReadOnlyList<AtsOrganizationAccess>> GetOrganizations(string atsBaseUrl, string apiKey, Guid? groupId = null)
    {
        if (string.IsNullOrEmpty(atsBaseUrl))
            throw new AtsApiException("ATS base URL is not configured for this group");

        try
        {
            var client = GetHttpClient(atsBaseUrl, apiKey);
            var url = "/api/v1/external/organizations";
            if (groupId.HasValue)
                url += $"?groupId={groupId.Value}";

            var response = await client.GetAsync(url).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new AtsApiException(
                    $"ATS API returned error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseResource = JsonSerializer.Deserialize<List<AtsOrganizationInfoResponse>>(responseContent, JsonOptions);

            if (responseResource == null)
                return Array.Empty<AtsOrganizationAccess>();

            return responseResource.Select(o => new AtsOrganizationAccess
            {
                Id = o.Id,
                GroupId = o.GroupId,
                Name = o.Name
            }).ToList();
        }
        catch (HttpRequestException ex)
        {
            throw new AtsConnectionException($"Failed to connect to ATS API at {atsBaseUrl}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new AtsConnectionException($"ATS API request timed out: {ex.Message}", ex);
        }
        catch (AtsApiException)
        {
            throw;
        }
        catch (AtsConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AtsApiException($"Unexpected error calling ATS API: {ex.Message}", ex);
        }
    }

    private static AtsUserAccess MapToUserAccess(AtsUserAccessResponse response)
    {
        return new AtsUserAccess
        {
            UserId = response.UserId,
            Auth0Sub = response.Auth0Sub,
            UserName = response.UserName,
            IsSuperadmin = response.IsSuperadmin,
            IsGroupAdmin = response.IsGroupAdmin,
            AdminGroupIds = response.AdminGroupIds,
            AccessibleGroups = response.AccessibleGroups.Select(g => new AtsGroupAccess
            {
                Id = g.Id,
                Name = g.Name
            }).ToList(),
            AccessibleOrganizations = response.AccessibleOrganizations.Select(o => new AtsOrganizationAccess
            {
                Id = o.Id,
                GroupId = o.GroupId,
                Name = o.Name
            }).ToList()
        };
    }

    public void Dispose()
    {
        foreach (var client in _httpClients.Values)
        {
            client.Dispose();
        }
        _httpClients.Clear();
    }
}
