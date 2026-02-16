using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    /// <summary>
    /// Gets a summary of orphaned entities in a group. An entity is orphaned when its
    /// organization_id is NOT NULL and is NOT in the provided list of known organization IDs.
    /// </summary>
    public async Task<OrphanedEntitySummary> GetOrphanedEntitySummary(Guid groupId, IReadOnlyList<Guid> knownOrganizationIds)
    {
        var knownIds = knownOrganizationIds.ToArray();

        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM agents WHERE group_id = @GroupId AND is_deleted = false AND organization_id IS NOT NULL AND organization_id != ALL(@KnownOrgIds)) AS orphaned_agent_count,
                (SELECT COUNT(*) FROM interview_guides WHERE group_id = @GroupId AND is_deleted = false AND organization_id IS NOT NULL AND organization_id != ALL(@KnownOrgIds)) AS orphaned_interview_guide_count,
                (SELECT COUNT(*) FROM interview_configurations WHERE group_id = @GroupId AND is_deleted = false AND organization_id IS NOT NULL AND organization_id != ALL(@KnownOrgIds)) AS orphaned_interview_configuration_count,
                (SELECT COUNT(*) FROM jobs WHERE group_id = @GroupId AND is_deleted = false AND organization_id IS NOT NULL AND organization_id != ALL(@KnownOrgIds)) AS orphaned_job_count,
                (SELECT COUNT(*) FROM applicants WHERE group_id = @GroupId AND is_deleted = false AND organization_id IS NOT NULL AND organization_id != ALL(@KnownOrgIds)) AS orphaned_applicant_count";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var result = await connection.QueryFirstAsync<OrphanedEntitySummary>(sql, new { GroupId = groupId, KnownOrgIds = knownIds });

        // Also find the distinct orphaned org IDs
        const string orphanedOrgSql = @"
            SELECT DISTINCT organization_id FROM (
                SELECT organization_id FROM agents WHERE group_id = @GroupId AND is_deleted = false AND organization_id IS NOT NULL AND organization_id != ALL(@KnownOrgIds)
                UNION
                SELECT organization_id FROM interview_guides WHERE group_id = @GroupId AND is_deleted = false AND organization_id IS NOT NULL AND organization_id != ALL(@KnownOrgIds)
                UNION
                SELECT organization_id FROM interview_configurations WHERE group_id = @GroupId AND is_deleted = false AND organization_id IS NOT NULL AND organization_id != ALL(@KnownOrgIds)
                UNION
                SELECT organization_id FROM jobs WHERE group_id = @GroupId AND is_deleted = false AND organization_id IS NOT NULL AND organization_id != ALL(@KnownOrgIds)
                UNION
                SELECT organization_id FROM applicants WHERE group_id = @GroupId AND is_deleted = false AND organization_id IS NOT NULL AND organization_id != ALL(@KnownOrgIds)
            ) AS orphaned_orgs";

        var orphanedOrgIds = await connection.QueryAsync<Guid>(orphanedOrgSql, new { GroupId = groupId, KnownOrgIds = knownIds });
        result.OrphanedOrganizationIds = orphanedOrgIds.ToList();

        return result;
    }
}
