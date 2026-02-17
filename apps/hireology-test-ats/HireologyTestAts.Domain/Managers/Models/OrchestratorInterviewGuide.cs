namespace HireologyTestAts.Domain;

/// <summary>
/// Represents an interview guide from the Orchestrator
/// </summary>
public class OrchestratorInterviewGuide
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int QuestionCount { get; set; }
    public bool IsActive { get; set; }
}
