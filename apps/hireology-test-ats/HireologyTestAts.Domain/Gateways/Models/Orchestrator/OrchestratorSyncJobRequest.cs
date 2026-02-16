using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Request model for syncing a job to the Orchestrator API
/// </summary>
internal class OrchestratorSyncJobRequest
{
    [JsonPropertyName("externalJobId")]
    public string ExternalJobId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("organizationId")]
    public Guid? OrganizationId { get; set; }
}
