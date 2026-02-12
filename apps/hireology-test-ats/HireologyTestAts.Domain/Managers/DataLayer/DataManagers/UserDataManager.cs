using Dapper;
using Npgsql;

namespace HireologyTestAts.Domain;

internal sealed class UserDataManager
{
    private readonly string _connectionString;

    public UserDataManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<User?> GetByAuth0SubAsync(string auth0Sub)
    {
        const string sql = @"
            SELECT id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users WHERE auth0_sub = @Auth0Sub";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { Auth0Sub = auth0Sub }));
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { Id = id }));
    }

    public async Task<User> CreateAsync(User user)
    {
        if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO users (id, auth0_sub, email, name, created_at, updated_at)
            VALUES (@Id, @Auth0Sub, @Email, @Name, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return (await conn.QuerySingleAsync<User>(new CommandDefinition(sql, user)))!;
    }

    public async Task<User?> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE users SET email = @Email, name = @Name, updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, user));
    }

    public async Task<IReadOnlyList<User>> ListAsync(int pageNumber, int pageSize)
    {
        var offset = (pageNumber - 1) * pageSize;
        const string sql = @"
            SELECT id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<User>(new CommandDefinition(sql, new { PageSize = pageSize, Offset = offset }));
        return items.ToList();
    }

    public async Task<int> CountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM users";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql));
    }
}
