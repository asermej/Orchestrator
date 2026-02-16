namespace Orchestrator.Domain;

/// <summary>
/// Summary of orphaned entities in a group -- entities whose organization_id
/// doesn't match any organization currently known in the ATS.
/// </summary>
public class OrphanedEntitySummary
{
    public int OrphanedAgentCount { get; set; }
    public int OrphanedInterviewGuideCount { get; set; }
    public int OrphanedInterviewConfigurationCount { get; set; }
    public int OrphanedJobCount { get; set; }
    public int OrphanedApplicantCount { get; set; }
    public int TotalOrphanedCount => OrphanedAgentCount + OrphanedInterviewGuideCount
        + OrphanedInterviewConfigurationCount + OrphanedJobCount + OrphanedApplicantCount;
    public IReadOnlyList<Guid> OrphanedOrganizationIds { get; set; } = Array.Empty<Guid>();
}
