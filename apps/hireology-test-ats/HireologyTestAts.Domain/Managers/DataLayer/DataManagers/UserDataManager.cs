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
            SELECT id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name,
                   is_superadmin AS IsSuperadmin, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users WHERE auth0_sub = @Auth0Sub";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { Auth0Sub = auth0Sub }));
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name,
                   is_superadmin AS IsSuperadmin, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { Id = id }));
    }

    public async Task<User> CreateAsync(User user)
    {
        if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        // Store NULL in DB for empty auth0_sub (pre-provisioned users) so partial unique index works
        var auth0SubParam = string.IsNullOrEmpty(user.Auth0Sub) ? (string?)null : user.Auth0Sub;
        const string sql = @"
            INSERT INTO users (id, auth0_sub, email, name, is_superadmin, created_at, updated_at)
            VALUES (@Id, @Auth0Sub, @Email, @Name, @IsSuperadmin, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name,
                      is_superadmin AS IsSuperadmin, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return (await conn.QuerySingleAsync<User>(new CommandDefinition(sql, new
        {
            user.Id,
            Auth0Sub = auth0SubParam,
            user.Email,
            user.Name,
            user.IsSuperadmin,
            user.CreatedAt,
            user.UpdatedAt
        })))!;
    }

    public async Task<User?> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE users SET email = @Email, name = @Name, updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name,
                      is_superadmin AS IsSuperadmin, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, user));
    }

    public async Task<IReadOnlyList<User>> ListAsync(int pageNumber, int pageSize)
    {
        var offset = (pageNumber - 1) * pageSize;
        const string sql = @"
            SELECT id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name,
                   is_superadmin AS IsSuperadmin, created_at AS CreatedAt, updated_at AS UpdatedAt
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

    public async Task<bool> SetSuperadminAsync(Guid userId, bool isSuperadmin)
    {
        const string sql = @"
            UPDATE users SET is_superadmin = @IsSuperadmin, updated_at = @UpdatedAt
            WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = userId,
            IsSuperadmin = isSuperadmin,
            UpdatedAt = DateTime.UtcNow
        }));
        return rows > 0;
    }

    public async Task<IReadOnlyList<User>> GetSuperadminsAsync()
    {
        const string sql = @"
            SELECT id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name,
                   is_superadmin AS IsSuperadmin, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users WHERE is_superadmin = true ORDER BY created_at";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<User>(new CommandDefinition(sql));
        return items.ToList();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = @"
            SELECT id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name,
                   is_superadmin AS IsSuperadmin, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users WHERE LOWER(email) = LOWER(@Email)";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { Email = email }));
    }

    public async Task<User?> UpdateAuth0SubAsync(Guid userId, string auth0Sub, string? name)
    {
        var now = DateTime.UtcNow;
        const string sql = @"
            UPDATE users SET auth0_sub = @Auth0Sub, name = COALESCE(@Name, name), updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING id AS Id, auth0_sub AS Auth0Sub, email AS Email, name AS Name,
                      is_superadmin AS IsSuperadmin, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { Id = userId, Auth0Sub = auth0Sub, Name = name, UpdatedAt = now }));
    }

    public async Task<IReadOnlyList<User>> GetUsersByGroupAsync(Guid groupId)
    {
        const string sql = @"
            SELECT u.id AS Id, u.auth0_sub AS Auth0Sub, u.email AS Email, u.name AS Name,
                   u.is_superadmin AS IsSuperadmin, u.created_at AS CreatedAt, u.updated_at AS UpdatedAt
            FROM users u
            INNER JOIN user_group_access uga ON uga.user_id = u.id
            WHERE uga.group_id = @GroupId
            ORDER BY u.email, u.name";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<User>(new CommandDefinition(sql, new { GroupId = groupId }));
        return items.ToList();
    }
}
