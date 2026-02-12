using Dapper;
using Npgsql;
using HireologyTestAts.Api.Models;

namespace HireologyTestAts.Api.Services;

/// <summary>
/// Resolves which organizations a user can access (via group membership or direct org access).
/// Parent (group) access implies all child organizations.
/// </summary>
public class UserAccessService
{
    private readonly string _connectionString;

    public UserAccessService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HireologyTestAts")
            ?? throw new InvalidOperationException("ConnectionStrings:HireologyTestAts is required");
    }

    /// <summary>
    /// All organization IDs the user can access: orgs from groups they belong to + orgs they have direct access to.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GetAllowedOrganizationIdsAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT DISTINCT o.id
            FROM organizations o
            INNER JOIN groups g ON g.id = o.group_id
            INNER JOIN user_group_access uga ON uga.group_id = g.id
            WHERE uga.user_id = @UserId
            UNION
            SELECT uoa.organization_id FROM user_organization_access uoa WHERE uoa.user_id = @UserId";
        await using var conn = new NpgsqlConnection(_connectionString);
        var ids = await conn.QueryAsync<Guid>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return ids.ToList();
    }

    /// <summary>
    /// Groups the user has access to (for switcher).
    /// </summary>
    public async Task<IReadOnlyList<GroupItem>> GetAccessibleGroupsAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT g.id AS Id, g.name AS Name, g.created_at AS CreatedAt, g.updated_at AS UpdatedAt
            FROM groups g
            INNER JOIN user_group_access uga ON uga.group_id = g.id
            WHERE uga.user_id = @UserId
            ORDER BY g.name";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<GroupItem>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return items.ToList();
    }

    /// <summary>
    /// All organizations the user can access (via groups or direct), with group info for grouping in the switcher.
    /// </summary>
    public async Task<IReadOnlyList<OrganizationItem>> GetAccessibleOrganizationsAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM (
                SELECT DISTINCT o.id AS Id, o.group_id AS GroupId, o.name AS Name, o.city AS City, o.state AS State, o.created_at AS CreatedAt, o.updated_at AS UpdatedAt
                FROM organizations o
                INNER JOIN groups g ON g.id = o.group_id
                INNER JOIN user_group_access uga ON uga.group_id = g.id
                WHERE uga.user_id = @UserId
                UNION
                SELECT o.id AS Id, o.group_id AS GroupId, o.name AS Name, o.city AS City, o.state AS State, o.created_at AS CreatedAt, o.updated_at AS UpdatedAt
                FROM organizations o
                INNER JOIN user_organization_access uoa ON uoa.organization_id = o.id
                WHERE uoa.user_id = @UserId
            ) AS u ORDER BY name";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<OrganizationItem>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return items.ToList();
    }

    /// <summary>
    /// Returns true if the user is allowed to access the given organization.
    /// </summary>
    public async Task<bool> CanAccessOrganizationAsync(Guid userId, Guid organizationId, CancellationToken ct = default)
    {
        var allowed = await GetAllowedOrganizationIdsAsync(userId, ct);
        return allowed.Contains(organizationId);
    }
}
