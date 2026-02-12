using Dapper;
using Npgsql;

namespace HireologyTestAts.Domain;

internal sealed class OrganizationDataManager
{
    private readonly string _connectionString;

    public OrganizationDataManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<Organization>> ListAsync(Guid? groupId)
    {
        const string sqlBase = @"
            SELECT id AS Id, group_id AS GroupId, name AS Name, city AS City, state AS State,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM organizations";
        var sql = groupId.HasValue
            ? sqlBase + " WHERE group_id = @GroupId ORDER BY name"
            : sqlBase + " ORDER BY name";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<Organization>(
            new CommandDefinition(sql, new { GroupId = groupId }));
        return items.ToList();
    }

    public async Task<Organization?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id AS Id, group_id AS GroupId, name AS Name, city AS City, state AS State,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM organizations WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Organization>(new CommandDefinition(sql, new { Id = id }));
    }

    public async Task<Organization> CreateAsync(Organization org)
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
        return (await conn.QuerySingleAsync<Organization>(new CommandDefinition(sql, org)))!;
    }

    public async Task<Organization?> UpdateAsync(Organization org)
    {
        org.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE organizations SET group_id = @GroupId, name = @Name, city = @City, state = @State, updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING id AS Id, group_id AS GroupId, name AS Name, city AS City, state AS State,
                      created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Organization>(new CommandDefinition(sql, org));
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM organizations WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }));
        return rows > 0;
    }
}
