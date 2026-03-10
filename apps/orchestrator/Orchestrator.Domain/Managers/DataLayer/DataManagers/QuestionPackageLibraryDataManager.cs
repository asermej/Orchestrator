using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

internal sealed class QuestionPackageLibraryDataManager
{
    private readonly string _dbConnectionString;

    public QuestionPackageLibraryDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<RoleTemplate>();
        DapperConfiguration.ConfigureSnakeCaseMapping<Competency>();
    }

    private const string RoleTemplateColumns = @"id, role_key, role_name, industry, source, group_id, organization_id, visibility_scope,
                   max_follow_ups_per_question, scoring_scale_min, scoring_scale_max, flag_threshold,
                   created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by";

    public async Task<List<RoleTemplate>> GetAllRoleTemplates()
    {
        var sql = $@"
            SELECT {RoleTemplateColumns}
            FROM role_templates
            WHERE is_deleted = false
            ORDER BY role_name";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var results = await connection.QueryAsync<RoleTemplate>(sql);
        return results.ToList();
    }

    public async Task<List<RoleTemplate>> GetRoleTemplatesByFilter(string? source = null, Guid? groupId = null)
    {
        var whereClauses = new List<string> { "rt.is_deleted = false" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(source))
        {
            whereClauses.Add("rt.source = @source");
            parameters.Add("source", source);
        }

        if (groupId.HasValue)
        {
            whereClauses.Add("(rt.group_id = @groupId OR rt.source = 'system')");
            parameters.Add("groupId", groupId.Value);
        }

        var sql = $@"
            SELECT rt.id, rt.role_key, rt.role_name, rt.industry, rt.source, rt.group_id, rt.organization_id, rt.visibility_scope,
                   rt.max_follow_ups_per_question, rt.scoring_scale_min, rt.scoring_scale_max, rt.flag_threshold,
                   rt.created_at, rt.updated_at, rt.created_by, rt.updated_by, rt.is_deleted, rt.deleted_at, rt.deleted_by,
                   (SELECT COUNT(*)::int FROM competencies c WHERE c.role_template_id = rt.id AND c.is_deleted = false) AS competency_count
            FROM role_templates rt
            WHERE {string.Join(" AND ", whereClauses)}
            ORDER BY rt.source, rt.role_name";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var results = await connection.QueryAsync<RoleTemplate>(sql, parameters);
        return results.ToList();
    }

    public async Task<RoleTemplate?> GetRoleTemplateByKey(string roleKey)
    {
        var sql = $@"
            SELECT {RoleTemplateColumns}
            FROM role_templates
            WHERE role_key = @roleKey AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<RoleTemplate>(sql, new { roleKey });
    }

    public async Task<RoleTemplate?> GetRoleTemplateById(Guid id)
    {
        var sql = $@"
            SELECT {RoleTemplateColumns}
            FROM role_templates
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<RoleTemplate>(sql, new { id });
    }

    public async Task<RoleTemplate> CreateRoleTemplate(RoleTemplate roleTemplate)
    {
        if (roleTemplate.Id == Guid.Empty)
            roleTemplate.Id = Guid.NewGuid();

        var sql = $@"
            INSERT INTO role_templates (id, role_key, role_name, industry, source, group_id, organization_id, visibility_scope,
                   max_follow_ups_per_question, scoring_scale_min, scoring_scale_max, flag_threshold, created_by)
            VALUES (@Id, @RoleKey, @RoleName, @Industry, @Source, @GroupId, @OrganizationId, @VisibilityScope,
                   @MaxFollowUpsPerQuestion, @ScoringScaleMin, @ScoringScaleMax, @FlagThreshold, @CreatedBy)
            RETURNING {RoleTemplateColumns}";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var created = await connection.QueryFirstOrDefaultAsync<RoleTemplate>(sql, roleTemplate);
        return created!;
    }

    public async Task<RoleTemplate> UpdateRoleTemplate(RoleTemplate roleTemplate)
    {
        var sql = $@"
            UPDATE role_templates
            SET role_name = @RoleName,
                industry = @Industry,
                visibility_scope = @VisibilityScope,
                max_follow_ups_per_question = @MaxFollowUpsPerQuestion,
                scoring_scale_min = @ScoringScaleMin,
                scoring_scale_max = @ScoringScaleMax,
                flag_threshold = @FlagThreshold,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING {RoleTemplateColumns}";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updated = await connection.QueryFirstOrDefaultAsync<RoleTemplate>(sql, roleTemplate);
        if (updated == null)
            throw new QuestionPackageLibraryNotFoundException("Role template not found or already deleted.");
        return updated;
    }

    public async Task<bool> DeleteRoleTemplate(Guid id, string? deletedBy = null)
    {
        const string sql = @"
            UPDATE role_templates
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP, deleted_by = @deletedBy
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id, deletedBy });
        return rowsAffected > 0;
    }

    public async Task SoftDeleteChildrenOfRoleTemplate(Guid roleTemplateId, string? deletedBy = null)
    {
        const string sql = @"
            UPDATE competencies SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP, deleted_by = @deletedBy
            WHERE role_template_id = @roleTemplateId AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        await connection.ExecuteAsync(sql, new { roleTemplateId, deletedBy });
    }

    public async Task<List<Competency>> GetCompetenciesByRoleTemplateId(Guid roleTemplateId)
    {
        const string sql = @"
            SELECT id, role_template_id, competency_key, name, description, default_weight, is_required, display_order,
                   canonical_example,
                   created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by
            FROM competencies
            WHERE role_template_id = @roleTemplateId AND is_deleted = false
            ORDER BY display_order";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var results = await connection.QueryAsync<Competency>(sql, new { roleTemplateId });
        return results.ToList();
    }

    public async Task<Competency> CreateCompetency(Competency competency)
    {
        if (competency.Id == Guid.Empty)
            competency.Id = Guid.NewGuid();

        const string sql = @"
            INSERT INTO competencies (id, role_template_id, competency_key, name, description, default_weight, is_required, display_order, canonical_example, created_by)
            VALUES (@Id, @RoleTemplateId, @CompetencyKey, @Name, @Description, @DefaultWeight, @IsRequired, @DisplayOrder, @CanonicalExample, @CreatedBy)
            RETURNING id, role_template_id, competency_key, name, description, default_weight, is_required, display_order, canonical_example,
                      created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var created = await connection.QueryFirstOrDefaultAsync<Competency>(sql, competency);
        return created!;
    }

    public async Task<Competency> UpdateCompetency(Competency competency)
    {
        const string sql = @"
            UPDATE competencies
            SET name = @Name, description = @Description, default_weight = @DefaultWeight, is_required = @IsRequired,
                display_order = @DisplayOrder, canonical_example = @CanonicalExample, updated_at = CURRENT_TIMESTAMP, updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, role_template_id, competency_key, name, description, default_weight, is_required, display_order, canonical_example,
                      created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updated = await connection.QueryFirstOrDefaultAsync<Competency>(sql, competency);
        if (updated == null)
            throw new QuestionPackageLibraryNotFoundException("Competency not found or already deleted.");
        return updated;
    }

    public async Task<bool> DeleteCompetency(Guid id, string? deletedBy = null)
    {
        const string sql = @"
            UPDATE competencies SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP, deleted_by = @deletedBy
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id, deletedBy });
        return rowsAffected > 0;
    }

    /// <summary>
    /// Loads a full role template with all competencies.
    /// </summary>
    public async Task<RoleTemplate?> GetRoleTemplateWithFullDetails(string roleKey)
    {
        var roleTemplate = await GetRoleTemplateByKey(roleKey);
        if (roleTemplate == null) return null;

        roleTemplate.Competencies = await GetCompetenciesByRoleTemplateId(roleTemplate.Id);
        return roleTemplate;
    }

    /// <summary>
    /// Loads a full role template by ID with all competencies.
    /// </summary>
    public async Task<RoleTemplate?> GetRoleTemplateWithFullDetailsById(Guid id)
    {
        var roleTemplate = await GetRoleTemplateById(id);
        if (roleTemplate == null) return null;

        roleTemplate.Competencies = await GetCompetenciesByRoleTemplateId(roleTemplate.Id);
        return roleTemplate;
    }

    // --- Org-scoped search methods ---

    private const string RoleTemplateListColumns = @"rt.id, rt.role_key, rt.role_name, rt.industry, rt.source, rt.group_id, rt.organization_id, rt.visibility_scope,
                   rt.max_follow_ups_per_question, rt.scoring_scale_min, rt.scoring_scale_max, rt.flag_threshold,
                   rt.created_at, rt.updated_at, rt.created_by, rt.updated_by, rt.is_deleted, rt.deleted_at, rt.deleted_by,
                   (SELECT COUNT(*)::int FROM competencies c WHERE c.role_template_id = rt.id AND c.is_deleted = false) AS competency_count";

    public async Task<List<RoleTemplate>> SearchLocal(Guid groupId, Guid organizationId)
    {
        var sql = $@"
            SELECT {RoleTemplateListColumns}
            FROM role_templates rt
            WHERE rt.group_id = @groupId
              AND rt.organization_id = @organizationId
              AND rt.source = 'custom'
              AND rt.is_deleted = false
            ORDER BY rt.role_name";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var results = await connection.QueryAsync<RoleTemplate>(sql, new { groupId, organizationId });
        return results.ToList();
    }

    public async Task<List<RoleTemplate>> SearchInherited(Guid groupId, IReadOnlyList<Guid> ancestorOrgIds)
    {
        var sql = $@"
            SELECT {RoleTemplateListColumns}
            FROM role_templates rt
            WHERE rt.group_id = @groupId
              AND rt.source = 'custom'
              AND rt.is_deleted = false
              AND (
                  (rt.organization_id = ANY(@ancestorOrgIds) AND rt.visibility_scope IN ('organization_and_descendants', 'descendants_only'))
                  OR (rt.organization_id IS NULL)
              )
            ORDER BY rt.role_name";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var results = await connection.QueryAsync<RoleTemplate>(sql, new { groupId, ancestorOrgIds });
        return results.ToList();
    }

    public async Task<List<RoleTemplate>> SearchSystem()
    {
        var sql = $@"
            SELECT {RoleTemplateListColumns}
            FROM role_templates rt
            WHERE rt.source = 'system'
              AND rt.is_deleted = false
            ORDER BY rt.role_name";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var results = await connection.QueryAsync<RoleTemplate>(sql);
        return results.ToList();
    }

    /// <summary>
    /// Deep-clones a role template (with all competencies) into a target organization.
    /// </summary>
    public async Task<RoleTemplate> CloneRoleTemplate(Guid roleTemplateId, Guid targetOrganizationId, Guid targetGroupId, string? createdBy = null)
    {
        var source = await GetRoleTemplateWithFullDetailsById(roleTemplateId);
        if (source == null)
            throw new QuestionPackageLibraryNotFoundException($"Role template {roleTemplateId} not found.");

        var clone = new RoleTemplate
        {
            Id = Guid.NewGuid(),
            RoleKey = source.RoleKey + "_" + Guid.NewGuid().ToString("N")[..6],
            RoleName = source.RoleName,
            Industry = source.Industry,
            Source = "custom",
            GroupId = targetGroupId,
            OrganizationId = targetOrganizationId,
            VisibilityScope = VisibilityScope.OrganizationOnly,
            MaxFollowUpsPerQuestion = source.MaxFollowUpsPerQuestion,
            ScoringScaleMin = source.ScoringScaleMin,
            ScoringScaleMax = source.ScoringScaleMax,
            FlagThreshold = source.FlagThreshold,
            CreatedBy = createdBy
        };

        var createdRole = await CreateRoleTemplate(clone);

        foreach (var srcCompetency in source.Competencies)
        {
            var clonedCompetency = new Competency
            {
                Id = Guid.NewGuid(),
                RoleTemplateId = createdRole.Id,
                CompetencyKey = srcCompetency.CompetencyKey,
                Name = srcCompetency.Name,
                Description = srcCompetency.Description,
                CanonicalExample = srcCompetency.CanonicalExample,
                DefaultWeight = srcCompetency.DefaultWeight,
                IsRequired = srcCompetency.IsRequired,
                DisplayOrder = srcCompetency.DisplayOrder,
                CreatedBy = createdBy
            };

            await CreateCompetency(clonedCompetency);
        }

        return (await GetRoleTemplateWithFullDetailsById(createdRole.Id))!;
    }
}
