namespace HireologyTestAts.Domain;

/// <summary>
/// Domain model representing the result of syncing a group to Orchestrator
/// </summary>
public class OrchestratorSyncGroupResult
{
    public Guid OrchestratorGroupId { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public bool IsNew { get; set; }
}
