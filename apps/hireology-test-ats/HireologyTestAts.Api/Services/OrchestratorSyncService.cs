using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HireologyTestAts.Api.Models;

namespace HireologyTestAts.Api.Services;

public class OrchestratorSyncService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public OrchestratorSyncService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _baseUrl = configuration["HireologyAts:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5000";
        _apiKey = configuration["HireologyAts:ApiKey"] ?? "";
    }

    public async Task<bool> SyncJobToOrchestratorAsync(JobItem job, CancellationToken ct = default)
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

        var response = await _http.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteJobFromOrchestratorAsync(string externalJobId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return false;

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/api/v1/ats/jobs/{Uri.EscapeDataString(externalJobId)}");
        request.Headers.Add("X-API-Key", _apiKey);

        var response = await _http.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }
}
