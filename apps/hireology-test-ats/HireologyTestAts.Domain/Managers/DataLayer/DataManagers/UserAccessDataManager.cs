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
                "INSERT INTO user_group_access (user_id, group_id, is_admin) VALUES (@UserId, @GroupId, false)",
                new { UserId = userId, GroupId = gid }));
        }
    }

    public async Task SetGroupAccessWithAdminAsync(Guid userId, IReadOnlyList<GroupAccessEntry> entries)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM user_group_access WHERE user_id = @UserId", new { UserId = userId }));
        foreach (var entry in entries.DistinctBy(e => e.GroupId))
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO user_group_access (user_id, group_id, is_admin) VALUES (@UserId, @GroupId, @IsAdmin)",
                new { UserId = userId, GroupId = entry.GroupId, IsAdmin = entry.IsAdmin }));
        }
    }

    public async Task AddGroupAccessAsync(Guid userId, Guid groupId, bool isAdmin)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO user_group_access (user_id, group_id, is_admin) VALUES (@UserId, @GroupId, @IsAdmin)
              ON CONFLICT (user_id, group_id) DO UPDATE SET is_admin = @IsAdmin",
            new { UserId = userId, GroupId = groupId, IsAdmin = isAdmin }));
    }

    public async Task RemoveGroupAccessAsync(Guid userId, Guid groupId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM user_group_access WHERE user_id = @UserId AND group_id = @GroupId",
            new { UserId = userId, GroupId = groupId }));
    }

    public async Task SetOrganizationAccessAsync(Guid userId, IReadOnlyList<Guid> organizationIds)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM user_organization_access WHERE user_id = @UserId", new { UserId = userId }));
        foreach (var oid in organizationIds.Distinct())
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO user_organization_access (user_id, organization_id, include_children) VALUES (@UserId, @OrganizationId, false)",
                new { UserId = userId, OrganizationId = oid }));
        }
    }

    public async Task SetOrganizationAccessWithFlagsAsync(Guid userId, IReadOnlyList<OrganizationAccessEntry> entries)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM user_organization_access WHERE user_id = @UserId", new { UserId = userId }));
        foreach (var entry in entries.DistinctBy(e => e.OrganizationId))
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO user_organization_access (user_id, organization_id, include_children) VALUES (@UserId, @OrganizationId, @IncludeChildren)",
                new { UserId = userId, OrganizationId = entry.OrganizationId, IncludeChildren = entry.IncludeChildren }));
        }
    }

    public async Task<bool> IsGroupAdminAsync(Guid userId, Guid groupId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM user_group_access
            WHERE user_id = @UserId AND group_id = @GroupId AND is_admin = true";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { UserId = userId, GroupId = groupId })) > 0;
    }

    public async Task<IReadOnlyList<Guid>> GetGroupAdminGroupIdsAsync(Guid userId)
    {
        const string sql = "SELECT group_id FROM user_group_access WHERE user_id = @UserId AND is_admin = true";
        await using var conn = new NpgsqlConnection(_connectionString);
        var ids = await conn.QueryAsync<Guid>(new CommandDefinition(sql, new { UserId = userId }));
        return ids.ToList();
    }

    public async Task<IReadOnlyList<OrganizationAccessEntry>> GetOrganizationAccessEntriesAsync(Guid userId)
    {
        const string sql = @"
            SELECT organization_id AS OrganizationId, include_children AS IncludeChildren
            FROM user_organization_access WHERE user_id = @UserId";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<OrganizationAccessEntry>(new CommandDefinition(sql, new { UserId = userId }));
        return items.ToList();
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
            WITH RECURSIVE subtree_roots AS (
                SELECT uoa.organization_id AS id
                FROM user_organization_access uoa
                WHERE uoa.user_id = @UserId AND uoa.include_children = true
            ),
            descendants AS (
                SELECT id FROM subtree_roots
                UNION ALL
                SELECT c.id FROM organizations c
                INNER JOIN descendants d ON c.parent_organization_id = d.id
            )
            SELECT DISTINCT o.id
            FROM organizations o
            INNER JOIN groups g ON g.id = o.group_id
            INNER JOIN user_group_access uga ON uga.group_id = g.id
            WHERE uga.user_id = @UserId
            UNION
            SELECT uoa.organization_id FROM user_organization_access uoa
            WHERE uoa.user_id = @UserId AND uoa.include_children = false
            UNION
            SELECT id FROM descendants";
        await using var conn = new NpgsqlConnection(_connectionString);
        var ids = await conn.QueryAsync<Guid>(new CommandDefinition(sql, new { UserId = userId }));
        return ids.ToList();
    }

    public async Task<IReadOnlyList<Group>> GetAccessibleGroupsAsync(Guid userId)
    {
        const string sql = @"
            SELECT g.id AS Id, g.root_organization_id AS RootOrganizationId,
                   g.name AS Name, g.created_at AS CreatedAt, g.updated_at AS UpdatedAt
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
            WITH RECURSIVE subtree_roots AS (
                SELECT uoa.organization_id AS id
                FROM user_organization_access uoa
                WHERE uoa.user_id = @UserId AND uoa.include_children = true
            ),
            descendants AS (
                SELECT id FROM subtree_roots
                UNION ALL
                SELECT c.id FROM organizations c
                INNER JOIN descendants d ON c.parent_organization_id = d.id
            )
            SELECT * FROM (
                SELECT DISTINCT o.id AS Id, o.group_id AS GroupId, o.parent_organization_id AS ParentOrganizationId,
                       o.name AS Name, o.city AS City, o.state AS State,
                       o.created_at AS CreatedAt, o.updated_at AS UpdatedAt
                FROM organizations o
                INNER JOIN groups g ON g.id = o.group_id
                INNER JOIN user_group_access uga ON uga.group_id = g.id
                WHERE uga.user_id = @UserId
                UNION
                SELECT o.id AS Id, o.group_id AS GroupId, o.parent_organization_id AS ParentOrganizationId,
                       o.name AS Name, o.city AS City, o.state AS State,
                       o.created_at AS CreatedAt, o.updated_at AS UpdatedAt
                FROM organizations o
                INNER JOIN user_organization_access uoa ON uoa.organization_id = o.id
                WHERE uoa.user_id = @UserId AND uoa.include_children = false
                UNION
                SELECT o.id AS Id, o.group_id AS GroupId, o.parent_organization_id AS ParentOrganizationId,
                       o.name AS Name, o.city AS City, o.state AS State,
                       o.created_at AS CreatedAt, o.updated_at AS UpdatedAt
                FROM organizations o
                INNER JOIN descendants d ON d.id = o.id
            ) AS u ORDER BY name";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<Organization>(new CommandDefinition(sql, new { UserId = userId }));
        return items.ToList();
    }
}
