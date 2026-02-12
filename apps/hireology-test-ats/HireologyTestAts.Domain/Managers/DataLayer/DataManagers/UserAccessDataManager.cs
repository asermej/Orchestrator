using Dapper;
using Npgsql;

namespace HireologyTestAts.Domain;

internal sealed class UserAccessDataManager
{
    private readonly string _connectionString;

    public UserAccessDataManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SetGroupAccessAsync(Guid userId, IReadOnlyList<Guid> groupIds)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM user_group_access WHERE user_id = @UserId", new { UserId = userId }));
        foreach (var gid in groupIds.Distinct())
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO user_group_access (user_id, group_id) VALUES (@UserId, @GroupId)",
                new { UserId = userId, GroupId = gid }));
        }
    }

    public async Task SetOrganizationAccessAsync(Guid userId, IReadOnlyList<Guid> organizationIds)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM user_organization_access WHERE user_id = @UserId", new { UserId = userId }));
        foreach (var oid in organizationIds.Distinct())
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO user_organization_access (user_id, organization_id) VALUES (@UserId, @OrganizationId)",
                new { UserId = userId, OrganizationId = oid }));
        }
    }

    public async Task<IReadOnlyList<Guid>> GetGroupIdsAsync(Guid userId)
    {
        const string sql = "SELECT group_id FROM user_group_access WHERE user_id = @UserId";
        await using var conn = new NpgsqlConnection(_connectionString);
        var ids = await conn.QueryAsync<Guid>(new CommandDefinition(sql, new { UserId = userId }));
        return ids.ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetOrganizationIdsAsync(Guid userId)
    {
        const string sql = "SELECT organization_id FROM user_organization_access WHERE user_id = @UserId";
        await using var conn = new NpgsqlConnection(_connectionString);
        var ids = await conn.QueryAsync<Guid>(new CommandDefinition(sql, new { UserId = userId }));
        return ids.ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetAllowedOrganizationIdsAsync(Guid userId)
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
        var ids = await conn.QueryAsync<Guid>(new CommandDefinition(sql, new { UserId = userId }));
        return ids.ToList();
    }

    public async Task<IReadOnlyList<Group>> GetAccessibleGroupsAsync(Guid userId)
    {
        const string sql = @"
            SELECT g.id AS Id, g.name AS Name, g.created_at AS CreatedAt, g.updated_at AS UpdatedAt
            FROM groups g
            INNER JOIN user_group_access uga ON uga.group_id = g.id
            WHERE uga.user_id = @UserId
            ORDER BY g.name";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<Group>(new CommandDefinition(sql, new { UserId = userId }));
        return items.ToList();
    }

    public async Task<IReadOnlyList<Organization>> GetAccessibleOrganizationsAsync(Guid userId)
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
        var items = await conn.QueryAsync<Organization>(new CommandDefinition(sql, new { UserId = userId }));
        return items.ToList();
    }
}
