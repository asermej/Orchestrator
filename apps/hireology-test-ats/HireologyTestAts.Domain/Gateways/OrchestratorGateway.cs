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
