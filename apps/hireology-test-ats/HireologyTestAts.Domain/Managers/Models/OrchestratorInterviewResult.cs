namespace HireologyTestAts.Domain;

/// <summary>
/// Result from creating an interview in the Orchestrator
/// </summary>
public class OrchestratorCreateInterviewResult
{
    public Guid InterviewId { get; set; }
    public string InviteUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
}
