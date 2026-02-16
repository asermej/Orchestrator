using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Response model for getting the webhook URL from the Orchestrator API
/// </summary>
internal class OrchestratorGetWebhookResponse
{
    [JsonPropertyName("webhookUrl")]
    public string? WebhookUrl { get; set; }

    [JsonPropertyName("configured")]
    public bool Configured { get; set; }
}
