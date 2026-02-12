using Dapper;
using Npgsql;
using HireologyTestAts.Api.Models;

namespace HireologyTestAts.Api.Services;

public class UsersRepository
{
    private readonly string _connectionString;

    public UsersRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HireologyTestAts")
            ?? throw new InvalidOperationException("ConnectionStrings:HireologyTestAts is required");
    }

    public async Task<UserItem?> GetByAuth0SubAsync(string auth0Sub, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users WHERE auth0_sub = @Auth0Sub";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<UserItem>(new CommandDefinition(sql, new { Auth0Sub = auth0Sub }, cancellationToken: ct));
    }

    public async Task<UserItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<UserItem>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<UserItem> CreateAsync(UserItem user, CancellationToken ct = default)
    {
        if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO users (id, auth0_sub, email, name, created_at, updated_at)
            VALUES (@Id, @Auth0Sub, @Email, @Name, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return (await conn.QuerySingleAsync<UserItem>(new CommandDefinition(sql, user, cancellationToken: ct)))!;
    }

    public async Task<UserItem?> UpdateAsync(UserItem user, CancellationToken ct = default)
    {
        user.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE users SET email = @Email, name = @Name, updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<UserItem>(new CommandDefinition(sql, user, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<UserItem>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var offset = (pageNumber - 1) * pageSize;
        const string sql = @"
            SELECT id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<UserItem>(new CommandDefinition(sql, new { PageSize = pageSize, Offset = offset }, cancellationToken: ct));
        return items.ToList();
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(*) FROM users";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: ct));
    }
}
