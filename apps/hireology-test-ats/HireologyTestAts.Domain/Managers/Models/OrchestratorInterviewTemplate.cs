namespace HireologyTestAts.Domain;

/// <summary>
/// Represents an interview template from the Orchestrator
/// </summary>
public class OrchestratorInterviewTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AgentId { get; set; }
    public string? AgentDisplayName { get; set; }
    public bool IsActive { get; set; }
}
