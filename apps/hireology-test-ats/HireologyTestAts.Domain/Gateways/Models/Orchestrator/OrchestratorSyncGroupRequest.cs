using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Request model for syncing a group to the Orchestrator API
/// </summary>
internal class OrchestratorSyncGroupRequest
{
    [JsonPropertyName("externalGroupId")]
    public Guid ExternalGroupId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("atsBaseUrl")]
    public string? AtsBaseUrl { get; set; }

    [JsonPropertyName("webhookUrl")]
    public string? WebhookUrl { get; set; }

    [JsonPropertyName("atsApiKey")]
    public string? AtsApiKey { get; set; }
}
