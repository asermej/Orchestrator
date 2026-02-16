namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    private DataFacade? _orphanDataFacade;
    private DataFacade OrphanDataFacade => _orphanDataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    /// <summary>
    /// Gets a summary of orphaned entities for a group.
    /// Queries the ATS for the list of known organizations,
    /// then finds local entities with org_ids not in that list.
    /// </summary>
    /// <param name="groupId">The Orchestrator group ID</param>
    public async Task<OrphanedEntitySummary> GetOrphanedEntitySummary(Guid groupId)
    {
        // Get known organizations from ATS
        var atsOrgs = await GetOrganizationsFromAts(groupId).ConfigureAwait(false);
        var knownOrgIds = atsOrgs.Select(o => o.Id).ToList();

        return await OrphanDataFacade.GetOrphanedEntitySummary(groupId, knownOrgIds).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets orphaned entity summary using a pre-fetched list of known organization IDs.
    /// Use this when you already have the known org IDs (e.g., from user context).
    /// </summary>
    public async Task<OrphanedEntitySummary> GetOrphanedEntitySummary(Guid groupId, IReadOnlyList<Guid> knownOrganizationIds)
    {
        return await OrphanDataFacade.GetOrphanedEntitySummary(groupId, knownOrganizationIds).ConfigureAwait(false);
    }
}
