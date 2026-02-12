using Dapper;
using Npgsql;
using HireologyTestAts.Api.Models;

namespace HireologyTestAts.Api.Services;

public class GroupsRepository
{
    private readonly string _connectionString;

    public GroupsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HireologyTestAts")
            ?? throw new InvalidOperationException("ConnectionStrings:HireologyTestAts is required");
    }

    public async Task<IReadOnlyList<GroupItem>> ListAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM groups
            ORDER BY name";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<GroupItem>(new CommandDefinition(sql, cancellationToken: ct));
        return items.ToList();
    }

    public async Task<GroupItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM groups WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<GroupItem>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<GroupItem> CreateAsync(GroupItem group, CancellationToken ct = default)
    {
        if (group.Id == Guid.Empty) group.Id = Guid.NewGuid();
        group.CreatedAt = DateTime.UtcNow;
        group.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO groups (id, name, created_at, updated_at)
            VALUES (@Id, @Name, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return (await conn.QuerySingleAsync<GroupItem>(new CommandDefinition(sql, group, cancellationToken: ct)))!;
    }

    public async Task<GroupItem?> UpdateAsync(GroupItem group, CancellationToken ct = default)
    {
        group.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE groups SET name = @Name, updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING id AS Id, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<GroupItem>(new CommandDefinition(sql, group, cancellationToken: ct));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM groups WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return rows > 0;
    }
}
