using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Response model for an interview guide returned by the Orchestrator API
/// </summary>
internal class OrchestratorInterviewGuideResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("questionCount")]
    public int QuestionCount { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}
