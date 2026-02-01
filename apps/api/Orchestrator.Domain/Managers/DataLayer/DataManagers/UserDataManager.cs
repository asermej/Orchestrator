using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for User entities
/// </summary>
internal sealed class UserDataManager
{
    private readonly string _dbConnectionString;

    public UserDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<User>();
    }

    public async Task<User?> GetById(System.Guid id)
    {
        const string sql = @"
            SELECT id, first_name, last_name, email, phone, auth0_sub, created_at, updated_at, is_deleted
            FROM users
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { id });
    }

    public async Task<User?> GetByAuth0Sub(string auth0Sub)
    {
        const string sql = @"
            SELECT id, first_name, last_name, email, phone, auth0_sub, created_at, updated_at, is_deleted
            FROM users
            WHERE auth0_sub = @Auth0Sub AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Auth0Sub = auth0Sub });
    }

    public async Task<User> Add(User user)
    {
        if (user.Id == System.Guid.Empty)
        {
            user.Id = System.Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO users (id, first_name, last_name, email, phone, auth0_sub)
            VALUES (@Id, @FirstName, @LastName, @Email, @Phone, @Auth0Sub)
            RETURNING id, first_name, last_name, email, phone, auth0_sub, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<User>(sql, user);
        return newItem!;
    }

    public async Task<User> Update(User user)
    {
        const string sql = @"
            UPDATE users
            SET
                first_name = @FirstName,
                last_name = @LastName,
                email = @Email,
                phone = @Phone,
                auth0_sub = @Auth0Sub,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id AND is_deleted = false
            RETURNING id, first_name, last_name, email, phone, auth0_sub, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<User>(sql, user);
        if (updatedItem == null)
        {
            throw new UserNotFoundException("User not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(System.Guid id)
    {
        const string sql = @"
            UPDATE users
            SET is_deleted = true, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<User>> Search(string? phone, string? email, string? lastName, int pageNumber, int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(phone))
        {
            whereClauses.Add("phone ILIKE @Phone");
            parameters.Add("Phone", $"%{phone}%");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            whereClauses.Add("email ILIKE @Email");
            parameters.Add("Email", $"%{email}%");
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            whereClauses.Add("last_name ILIKE @LastName");
            parameters.Add("LastName", $"%{lastName}%");
        }

        whereClauses.Add("is_deleted = false");

        var whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var countSql = $"SELECT COUNT(*) FROM users {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT id, first_name, last_name, email, phone, auth0_sub, created_at, updated_at, is_deleted
            FROM users
            {whereSql}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<User>(querySql, parameters);

        return new PaginatedResult<User>(items, totalCount, pageNumber, pageSize);
    }
} 