using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for InterviewConfiguration entities
/// </summary>
internal sealed class InterviewConfigurationDataManager
{
    private readonly string _dbConnectionString;

    public InterviewConfigurationDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<InterviewConfiguration>();
    }

    public async Task<InterviewConfiguration?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, group_id, organization_id, interview_guide_id, agent_id, name, description, is_active,
                   created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by
            FROM interview_configurations
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewConfiguration>(sql, new { id });
    }

    public async Task<InterviewConfiguration> Add(InterviewConfiguration config)
    {
        if (config.Id == Guid.Empty)
        {
            config.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO interview_configurations (id, group_id, organization_id, interview_guide_id, agent_id, name, description, is_active, created_by)
            VALUES (@Id, @GroupId, @OrganizationId, @InterviewGuideId, @AgentId, @Name, @Description, @IsActive, @CreatedBy)
            RETURNING id, group_id, organization_id, interview_guide_id, agent_id, name, description, is_active,
                      created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<InterviewConfiguration>(sql, config);
        return newItem!;
    }

    public async Task<InterviewConfiguration> Update(InterviewConfiguration config)
    {
        const string sql = @"
            UPDATE interview_configurations
            SET
                name = @Name,
                description = @Description,
                interview_guide_id = @InterviewGuideId,
                is_active = @IsActive,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, group_id, organization_id, interview_guide_id, agent_id, name, description, is_active,
                      created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<InterviewConfiguration>(sql, config);
        if (updatedItem == null)
        {
            throw new InterviewConfigurationNotFoundException("Interview configuration not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id, string? deletedBy = null)
    {
        const string sql = @"
            UPDATE interview_configurations
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP, deleted_by = @deletedBy
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id, deletedBy });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<InterviewConfiguration>> Search(
        Guid? groupId, 
        Guid? agentId, 
        string? name, 
        bool? isActive,
        string? sortBy, 
        int pageNumber, 
        int pageSize,
        IReadOnlyList<Guid>? organizationIds = null)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (groupId.HasValue)
        {
            whereClauses.Add("group_id = @GroupId");
            parameters.Add("GroupId", groupId.Value);
        }

        if (organizationIds != null)
        {
            whereClauses.Add("(organization_id IS NULL OR organization_id = ANY(@OrganizationIds))");
            parameters.Add("OrganizationIds", organizationIds.ToArray());
        }

        if (agentId.HasValue)
        {
            whereClauses.Add("agent_id = @AgentId");
            parameters.Add("AgentId", agentId.Value);
        }

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

        var orderByClause = sortBy?.ToLowerInvariant() switch
        {
            "alphabetical" => "name ASC",
            "recent" => "created_at DESC",
            _ => "created_at DESC"
        };

        using var connection = new NpgsqlConnection(_dbConnectionString);
        
        var countSql = $"SELECT COUNT(*) FROM interview_configurations {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT ic.id, ic.group_id, ic.organization_id, ic.interview_guide_id, ic.agent_id, ic.name, ic.description, ic.is_active,
                   ic.created_at, ic.updated_at, ic.created_by, ic.updated_by, ic.is_deleted, ic.deleted_at, ic.deleted_by,
                   (SELECT COUNT(*) FROM interview_guide_questions igq WHERE igq.interview_guide_id = ic.interview_guide_id) as question_count
            FROM interview_configurations ic
            {whereSql.Replace("group_id", "ic.group_id").Replace("agent_id", "ic.agent_id").Replace("name", "ic.name").Replace("is_active", "ic.is_active").Replace("is_deleted", "ic.is_deleted")}
            ORDER BY {orderByClause.Replace("name", "ic.name").Replace("created_at", "ic.created_at")}
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<InterviewConfiguration>(querySql, parameters);

        return new PaginatedResult<InterviewConfiguration>(items, totalCount, pageNumber, pageSize);
    }
}
