using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Request model for creating an interview in the Orchestrator API
/// </summary>
internal class OrchestratorCreateInterviewRequest
{
    [JsonPropertyName("externalApplicantId")]
    public string ExternalApplicantId { get; set; } = string.Empty;

    [JsonPropertyName("externalJobId")]
    public string ExternalJobId { get; set; } = string.Empty;

    [JsonPropertyName("interviewTemplateId")]
    public Guid InterviewTemplateId { get; set; }

    /// <summary>
    /// Optional. When provided, used as the interview agent; otherwise resolved from the template.
    /// </summary>
    [JsonPropertyName("agentId")]
    public Guid? AgentId { get; set; }

    [JsonPropertyName("interviewType")]
    public string InterviewType { get; set; } = "voice";
}
