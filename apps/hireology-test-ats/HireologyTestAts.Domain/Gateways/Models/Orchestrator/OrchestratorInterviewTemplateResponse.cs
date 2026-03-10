using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Response model for an interview template returned by the Orchestrator API
/// </summary>
internal class OrchestratorInterviewTemplateResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("agentId")]
    public Guid? AgentId { get; set; }

    [JsonPropertyName("agentDisplayName")]
    public string? AgentDisplayName { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}
