using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

internal sealed class InterviewTemplateDataManager
{
    private readonly string _dbConnectionString;

    public InterviewTemplateDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<InterviewTemplate>();
    }

    public async Task<InterviewTemplate?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, group_id, organization_id, name, description, is_active,
                   role_template_id, agent_id, opening_template, closing_template,
                   created_at, updated_at, created_by, updated_by, is_deleted
            FROM interview_templates
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewTemplate>(sql, new { id });
    }

    public async Task<InterviewTemplate> Add(InterviewTemplate template)
    {
        if (template.Id == Guid.Empty)
        {
            template.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO interview_templates (id, group_id, organization_id, name, description, is_active,
                role_template_id, agent_id, opening_template, closing_template, created_by)
            VALUES (@Id, @GroupId, @OrganizationId, @Name, @Description, @IsActive,
                @RoleTemplateId, @AgentId, @OpeningTemplate, @ClosingTemplate, @CreatedBy)
            RETURNING id, group_id, organization_id, name, description, is_active,
                      role_template_id, agent_id, opening_template, closing_template,
                      created_at, updated_at, created_by, updated_by, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<InterviewTemplate>(sql, template);
        return newItem!;
    }

    public async Task<InterviewTemplate> Update(InterviewTemplate template)
    {
        const string sql = @"
            UPDATE interview_templates
            SET
                name = @Name,
                description = @Description,
                is_active = @IsActive,
                role_template_id = @RoleTemplateId,
                agent_id = @AgentId,
                opening_template = @OpeningTemplate,
                closing_template = @ClosingTemplate,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, group_id, organization_id, name, description, is_active,
                      role_template_id, agent_id, opening_template, closing_template,
                      created_at, updated_at, created_by, updated_by, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<InterviewTemplate>(sql, template);
        if (updatedItem == null)
        {
            throw new InterviewTemplateNotFoundException("Interview template not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id, string? deletedBy = null)
    {
        const string sql = @"
            UPDATE interview_templates
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP, deleted_by = @deletedBy
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id, deletedBy });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<InterviewTemplate>> Search(
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

        var countSql = $"SELECT COUNT(*) FROM interview_templates {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT id, group_id, organization_id, name, description, is_active,
                   role_template_id, agent_id, opening_template, closing_template,
                   created_at, updated_at, created_by, updated_by, is_deleted
            FROM interview_templates
            {whereSql}
            ORDER BY {orderByClause}
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<InterviewTemplate>(querySql, parameters);

        return new PaginatedResult<InterviewTemplate>(items, totalCount, pageNumber, pageSize);
    }
}
