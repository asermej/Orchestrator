using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Response model for getting interview status from the Orchestrator API
/// </summary>
internal class OrchestratorInterviewStatusResponse
{
    [JsonPropertyName("interview")]
    public OrchestratorInterviewStatusResponseInterview Interview { get; set; } = new();

    [JsonPropertyName("inviteStatus")]
    public string InviteStatus { get; set; } = "none";
}

internal class OrchestratorInterviewStatusResponseInterview
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "unknown";
}
