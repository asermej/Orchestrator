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

    public async Task<IReadOnlyList<Group>> ListAsync()
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM groups
            ORDER BY name";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<Group>(new CommandDefinition(sql));
        return items.ToList();
    }

    public async Task<Group?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM groups WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Group>(new CommandDefinition(sql, new { Id = id }));
    }

    public async Task<Group> CreateAsync(Group group)
    {
        if (group.Id == Guid.Empty) group.Id = Guid.NewGuid();
        group.CreatedAt = DateTime.UtcNow;
        group.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO groups (id, name, created_at, updated_at)
            VALUES (@Id, @Name, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return (await conn.QuerySingleAsync<Group>(new CommandDefinition(sql, group)))!;
    }

    public async Task<Group?> UpdateAsync(Group group)
    {
        group.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE groups SET name = @Name, updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING id AS Id, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Group>(new CommandDefinition(sql, group));
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM groups WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }));
        return rows > 0;
    }
}
