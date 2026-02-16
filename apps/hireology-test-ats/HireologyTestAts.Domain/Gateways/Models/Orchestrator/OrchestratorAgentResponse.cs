using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Response model for an agent returned by the Orchestrator API
/// </summary>
internal class OrchestratorAgentResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("profileImageUrl")]
    public string? ProfileImageUrl { get; set; }
}
