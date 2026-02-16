using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Response model from the Orchestrator group sync API
/// </summary>
internal class OrchestratorSyncGroupResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("externalGroupId")]
    public Guid ExternalGroupId { get; set; }

    [JsonPropertyName("isNew")]
    public bool IsNew { get; set; }
}
