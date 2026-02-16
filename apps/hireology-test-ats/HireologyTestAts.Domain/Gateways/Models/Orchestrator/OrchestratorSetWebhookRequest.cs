using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Request model for setting the webhook URL in the Orchestrator API
/// </summary>
internal class OrchestratorSetWebhookRequest
{
    [JsonPropertyName("webhookUrl")]
    public string WebhookUrl { get; set; } = string.Empty;
}
