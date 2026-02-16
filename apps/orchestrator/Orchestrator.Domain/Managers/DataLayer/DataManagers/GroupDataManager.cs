using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for Group entities
/// </summary>
internal sealed class GroupDataManager
{
    private readonly string _dbConnectionString;

    public GroupDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<Group>();
    }

    public async Task<Group?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, name, api_key, webhook_url, is_active, external_group_id, ats_base_url,
                   ats_api_key, created_at, updated_at, is_deleted
            FROM groups
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Group>(sql, new { id });
    }

    public async Task<Group?> GetByExternalGroupId(Guid externalGroupId)
    {
        const string sql = @"
            SELECT id, name, api_key, webhook_url, is_active, external_group_id, ats_base_url,
                   ats_api_key, created_at, updated_at, is_deleted
            FROM groups
            WHERE external_group_id = @ExternalGroupId AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Group>(sql, new { ExternalGroupId = externalGroupId });
    }

    public async Task<Group?> GetByApiKey(string apiKey)
    {
        const string sql = @"
            SELECT id, name, api_key, webhook_url, is_active, external_group_id, ats_base_url,
                   ats_api_key, created_at, updated_at, is_deleted
            FROM groups
            WHERE api_key = @ApiKey AND is_deleted = false AND is_active = true";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Group>(sql, new { ApiKey = apiKey });
    }

    public async Task<Group> Upsert(Guid externalGroupId, string name, string? atsBaseUrl, string? webhookUrl, string? atsApiKey = null)
    {
        var existing = await GetByExternalGroupId(externalGroupId);
        if (existing != null)
        {
            existing.Name = name;
            if (atsBaseUrl != null) existing.AtsBaseUrl = atsBaseUrl;
            if (webhookUrl != null) existing.WebhookUrl = webhookUrl;
            if (atsApiKey != null) existing.AtsApiKey = atsApiKey;
            return await Update(existing);
        }

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            ApiKey = Guid.NewGuid().ToString("N"),
            ExternalGroupId = externalGroupId,
            AtsBaseUrl = atsBaseUrl,
            WebhookUrl = webhookUrl,
            AtsApiKey = atsApiKey,
            IsActive = true
        };
        return await Add(group);
    }

    public async Task<Group> Add(Group group)
    {
        if (group.Id == Guid.Empty)
        {
            group.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO groups (id, name, api_key, webhook_url, is_active, external_group_id, ats_base_url, ats_api_key, created_by)
            VALUES (@Id, @Name, @ApiKey, @WebhookUrl, @IsActive, @ExternalGroupId, @AtsBaseUrl, @AtsApiKey, @CreatedBy)
            RETURNING id, name, api_key, webhook_url, is_active, external_group_id, ats_base_url,
                      ats_api_key, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<Group>(sql, group);
        return newItem!;
    }

    public async Task<Group> Update(Group group)
    {
        const string sql = @"
            UPDATE groups
            SET
                name = @Name,
                webhook_url = @WebhookUrl,
                is_active = @IsActive,
                external_group_id = @ExternalGroupId,
                ats_base_url = @AtsBaseUrl,
                ats_api_key = @AtsApiKey,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, name, api_key, webhook_url, is_active, external_group_id, ats_base_url,
                      ats_api_key, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<Group>(sql, group);
        if (updatedItem == null)
        {
            throw new GroupNotFoundException("Group not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE groups
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<Group>> Search(string? name, bool? isActive, int pageNumber, int pageSize)
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
        var countSql = $"SELECT COUNT(*) FROM groups {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT id, name, api_key, webhook_url, is_active, external_group_id, ats_base_url,
                   ats_api_key, created_at, updated_at, is_deleted
            FROM groups
            {whereSql}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<Group>(querySql, parameters);

        return new PaginatedResult<Group>(items, totalCount, pageNumber, pageSize);
    }
}
