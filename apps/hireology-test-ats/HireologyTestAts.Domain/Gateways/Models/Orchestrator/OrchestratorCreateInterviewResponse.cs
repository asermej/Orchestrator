using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Response model for creating an interview in the Orchestrator API
/// </summary>
internal class OrchestratorCreateInterviewResponse
{
    [JsonPropertyName("interview")]
    public OrchestratorCreateInterviewResponseInterview Interview { get; set; } = new();

    [JsonPropertyName("invite")]
    public OrchestratorCreateInterviewResponseInvite Invite { get; set; } = new();
}

internal class OrchestratorCreateInterviewResponseInterview
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
}

internal class OrchestratorCreateInterviewResponseInvite
{
    [JsonPropertyName("shortCode")]
    public string ShortCode { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
