using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for Organization entities
/// </summary>
internal sealed class OrganizationDataManager
{
    private readonly string _dbConnectionString;

    public OrganizationDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<Organization>();
    }

    public async Task<Organization?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, name, api_key, webhook_url, is_active, created_at, updated_at, is_deleted
            FROM organizations
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Organization>(sql, new { id });
    }

    public async Task<Organization?> GetByApiKey(string apiKey)
    {
        const string sql = @"
            SELECT id, name, api_key, webhook_url, is_active, created_at, updated_at, is_deleted
            FROM organizations
            WHERE api_key = @ApiKey AND is_deleted = false AND is_active = true";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Organization>(sql, new { ApiKey = apiKey });
    }

    public async Task<Organization> Add(Organization organization)
    {
        if (organization.Id == Guid.Empty)
        {
            organization.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO organizations (id, name, api_key, webhook_url, is_active, created_by)
            VALUES (@Id, @Name, @ApiKey, @WebhookUrl, @IsActive, @CreatedBy)
            RETURNING id, name, api_key, webhook_url, is_active, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<Organization>(sql, organization);
        return newItem!;
    }

    public async Task<Organization> Update(Organization organization)
    {
        const string sql = @"
            UPDATE organizations
            SET
                name = @Name,
                webhook_url = @WebhookUrl,
                is_active = @IsActive,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, name, api_key, webhook_url, is_active, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<Organization>(sql, organization);
        if (updatedItem == null)
        {
            throw new OrganizationNotFoundException("Organization not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE organizations
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<Organization>> Search(string? name, bool? isActive, int pageNumber, int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(name))
        {
            whereClauses.Add("name ILIKE @Name");
            parameters.Add("Name", $"%{name}%");
        }

        if (isActive.HasValue)
        {
            whereClauses.Add("is_active = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }

        whereClauses.Add("is_deleted = false");

        var whereSql = $"WHERE {string.Join(" AND ", whereClauses)}";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var countSql = $"SELECT COUNT(*) FROM organizations {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT id, name, api_key, webhook_url, is_active, created_at, updated_at, is_deleted
            FROM organizations
            {whereSql}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<Organization>(querySql, parameters);

        return new PaginatedResult<Organization>(items, totalCount, pageNumber, pageSize);
    }
}
