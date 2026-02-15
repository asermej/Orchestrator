namespace HireologyTestAts.Domain;

/// <summary>
/// Represents an agent (interview configuration) from the Orchestrator
/// </summary>
public class OrchestratorAgent
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
}
