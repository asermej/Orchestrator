using Dapper;
using Npgsql;

namespace HireologyTestAts.Domain;

internal sealed class GroupDataManager
{
    private readonly string _connectionString;

    public GroupDataManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    private const string SelectColumns = @"
            id AS Id, root_organization_id AS RootOrganizationId, name AS Name,
            orchestrator_api_key AS OrchestratorApiKey,
            created_at AS CreatedAt, updated_at AS UpdatedAt";

    public async Task<IReadOnlyList<Group>> ListAsync(bool excludeTestData = false)
    {
        var sql = $"SELECT {SelectColumns} FROM groups";
        if (excludeTestData)
        {
            sql += " WHERE name NOT LIKE 'TestGroup_%'";
        }
        sql += " ORDER BY name";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<Group>(new CommandDefinition(sql));
        return items.ToList();
    }

    public async Task<Group?> GetByIdAsync(Guid id)
    {
        var sql = $"SELECT {SelectColumns} FROM groups WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Group>(new CommandDefinition(sql, new { Id = id }));
    }

    public async Task<Group> CreateAsync(Group group)
    {
        if (group.Id == Guid.Empty) group.Id = Guid.NewGuid();
        group.CreatedAt = DateTime.UtcNow;
        group.UpdatedAt = DateTime.UtcNow;
        var sql = $@"
            INSERT INTO groups (id, root_organization_id, name, orchestrator_api_key, created_at, updated_at)
            VALUES (@Id, @RootOrganizationId, @Name, @OrchestratorApiKey, @CreatedAt, @UpdatedAt)
            RETURNING {SelectColumns}";
        await using var conn = new NpgsqlConnection(_connectionString);
        return (await conn.QuerySingleAsync<Group>(new CommandDefinition(sql, group)))!;
    }

    public async Task<Group?> UpdateAsync(Group group)
    {
        group.UpdatedAt = DateTime.UtcNow;
        var sql = $@"
            UPDATE groups SET root_organization_id = @RootOrganizationId, name = @Name,
                   orchestrator_api_key = @OrchestratorApiKey, updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING {SelectColumns}";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Group>(new CommandDefinition(sql, group));
    }

    public async Task UpdateOrchestratorApiKey(Guid groupId, string apiKey)
    {
        const string sql = @"
            UPDATE groups SET orchestrator_api_key = @ApiKey, updated_at = @UpdatedAt
            WHERE id = @GroupId";
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { GroupId = groupId, ApiKey = apiKey, UpdatedAt = DateTime.UtcNow }));
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM groups WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }));
        return rows > 0;
    }
}
