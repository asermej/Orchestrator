using System.Net.Http.Json;
using System.Text.Json;

namespace HireologyTestAts.Api.Services;

/// <summary>
/// Provisions test-ats users to Orchestrator so they exist there (create/upsert by Auth0 sub).
/// </summary>
public class OrchestratorUserProvisioningService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public OrchestratorUserProvisioningService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _baseUrl = configuration["HireologyAts:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5000";
    }

    /// <summary>
    /// Ensure the user exists in Orchestrator. If not found by Auth0 sub, creates them.
    /// </summary>
    public async Task ProvisionUserAsync(string auth0Sub, string? email, string? name, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(auth0Sub)) return;

        var getUrl = $"{_baseUrl}/api/v1/user/by-auth0-sub/{Uri.EscapeDataString(auth0Sub)}";
        var getResponse = await _http.GetAsync(getUrl, ct);
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

        var postResponse = await _http.PostAsJsonAsync($"{_baseUrl}/api/v1/user", createPayload, JsonOptions, ct);
        postResponse.EnsureSuccessStatusCode();
    }

    private static (string FirstName, string LastName) SplitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return ("", "");
        var parts = name.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? (parts[0], parts[1]) : (parts[0], "");
    }
}
