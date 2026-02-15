namespace HireologyTestAts.Domain;

/// <summary>
/// Represents an interview configuration from the Orchestrator.
/// Bundles an agent, question set, and scoring rubric.
/// </summary>
public class OrchestratorInterviewConfiguration
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AgentId { get; set; }
    public string? AgentDisplayName { get; set; }
    public int QuestionCount { get; set; }
}
