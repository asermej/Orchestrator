using Dapper;
using Npgsql;
using HireologyTestAts.Api.Models;

namespace HireologyTestAts.Api.Services;

public class OrganizationsRepository
{
    private readonly string _connectionString;

    public OrganizationsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HireologyTestAts")
            ?? throw new InvalidOperationException("ConnectionStrings:HireologyTestAts is required");
    }

    public async Task<IReadOnlyList<OrganizationItem>> ListAsync(Guid? groupId, CancellationToken ct = default)
    {
        const string sqlBase = @"
            SELECT id AS Id, group_id AS GroupId, name AS Name, city AS City, state AS State,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM organizations";
        var sql = groupId.HasValue
            ? sqlBase + " WHERE group_id = @GroupId ORDER BY name"
            : sqlBase + " ORDER BY name";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<OrganizationItem>(
            new CommandDefinition(sql, new { GroupId = groupId }, cancellationToken: ct));
        return items.ToList();
    }

    public async Task<OrganizationItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, group_id AS GroupId, name AS Name, city AS City, state AS State,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM organizations WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<OrganizationItem>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<OrganizationItem> CreateAsync(OrganizationItem org, CancellationToken ct = default)
    {
        if (org.Id == Guid.Empty) org.Id = Guid.NewGuid();
        org.CreatedAt = DateTime.UtcNow;
        org.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO organizations (id, group_id, name, city, state, created_at, updated_at)
            VALUES (@Id, @GroupId, @Name, @City, @State, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, group_id AS GroupId, name AS Name, city AS City, state AS State,
                      created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return (await conn.QuerySingleAsync<OrganizationItem>(new CommandDefinition(sql, org, cancellationToken: ct)))!;
    }

    public async Task<OrganizationItem?> UpdateAsync(OrganizationItem org, CancellationToken ct = default)
    {
        org.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE organizations SET group_id = @GroupId, name = @Name, city = @City, state = @State, updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING id AS Id, group_id AS GroupId, name AS Name, city AS City, state AS State,
                      created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<OrganizationItem>(new CommandDefinition(sql, org, cancellationToken: ct));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM organizations WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return rows > 0;
    }
}
