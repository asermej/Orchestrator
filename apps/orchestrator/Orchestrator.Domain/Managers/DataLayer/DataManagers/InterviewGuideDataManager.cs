using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for InterviewGuide entities
/// </summary>
internal sealed class InterviewGuideDataManager
{
    private readonly string _dbConnectionString;

    public InterviewGuideDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<InterviewGuide>();
        DapperConfiguration.ConfigureSnakeCaseMapping<InterviewGuideQuestion>();
    }

    public async Task<InterviewGuide?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, group_id, organization_id, name, description, opening_template, closing_template,
                   scoring_rubric, is_active, visibility_scope,
                   created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by
            FROM interview_guides
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewGuide>(sql, new { id });
    }

    public async Task<InterviewGuide?> GetByIdWithQuestions(Guid id)
    {
        const string guideSql = @"
            SELECT id, group_id, organization_id, name, description, opening_template, closing_template,
                   scoring_rubric, is_active, visibility_scope,
                   created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by
            FROM interview_guides
            WHERE id = @id AND is_deleted = false";

        const string questionsSql = @"
            SELECT id, interview_guide_id, question, display_order, scoring_weight, scoring_guidance,
                   follow_ups_enabled, max_follow_ups, created_at, updated_at
            FROM interview_guide_questions
            WHERE interview_guide_id = @id
            ORDER BY display_order";

        using var connection = new NpgsqlConnection(_dbConnectionString);

        var guide = await connection.QueryFirstOrDefaultAsync<InterviewGuide>(guideSql, new { id });
        if (guide == null) return null;

        var questions = await connection.QueryAsync<InterviewGuideQuestion>(questionsSql, new { id });
        guide.Questions = questions.ToList();

        return guide;
    }

    public async Task<InterviewGuide> Add(InterviewGuide guide)
    {
        if (guide.Id == Guid.Empty)
        {
            guide.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO interview_guides (id, group_id, organization_id, name, description, opening_template, closing_template, scoring_rubric, is_active, visibility_scope, created_by)
            VALUES (@Id, @GroupId, @OrganizationId, @Name, @Description, @OpeningTemplate, @ClosingTemplate, @ScoringRubric, @IsActive, @VisibilityScope, @CreatedBy)
            RETURNING id, group_id, organization_id, name, description, opening_template, closing_template, scoring_rubric, is_active, visibility_scope,
                      created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<InterviewGuide>(sql, guide);
        return newItem!;
    }

    public async Task<InterviewGuide> Update(InterviewGuide guide)
    {
        const string sql = @"
            UPDATE interview_guides
            SET
                name = @Name,
                description = @Description,
                opening_template = @OpeningTemplate,
                closing_template = @ClosingTemplate,
                scoring_rubric = @ScoringRubric,
                is_active = @IsActive,
                visibility_scope = @VisibilityScope,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, group_id, organization_id, name, description, opening_template, closing_template, scoring_rubric, is_active, visibility_scope,
                      created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<InterviewGuide>(sql, guide);
        if (updatedItem == null)
        {
            throw new InterviewGuideNotFoundException("Interview guide not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id, string? deletedBy = null)
    {
        const string sql = @"
            UPDATE interview_guides
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP, deleted_by = @deletedBy
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id, deletedBy });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<InterviewGuide>> Search(
        Guid? groupId,
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

        var countSql = $"SELECT COUNT(*) FROM interview_guides {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT ig.id, ig.group_id, ig.organization_id, ig.name, ig.description, ig.opening_template, ig.closing_template,
                   ig.scoring_rubric, ig.is_active, ig.visibility_scope,
                   ig.created_at, ig.updated_at, ig.created_by, ig.updated_by, ig.is_deleted, ig.deleted_at, ig.deleted_by,
                   (SELECT COUNT(*) FROM interview_guide_questions igq WHERE igq.interview_guide_id = ig.id) as question_count
            FROM interview_guides ig
            {whereSql.Replace("group_id", "ig.group_id").Replace("name", "ig.name").Replace("is_active", "ig.is_active").Replace("is_deleted", "ig.is_deleted")}
            ORDER BY {orderByClause.Replace("name", "ig.name").Replace("created_at", "ig.created_at")}
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<InterviewGuide>(querySql, parameters);

        return new PaginatedResult<InterviewGuide>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Searches for local interview guides (created at the specified organization).
    /// </summary>
    public async Task<PaginatedResult<InterviewGuide>> SearchLocal(
        Guid groupId,
        Guid organizationId,
        string? name,
        bool? isActive,
        string? sortBy,
        int pageNumber,
        int pageSize)
    {
        var whereClauses = new List<string>
        {
            "ig.group_id = @GroupId",
            "ig.organization_id = @OrganizationId",
            "ig.is_deleted = false"
        };
        var parameters = new DynamicParameters();
        parameters.Add("GroupId", groupId);
        parameters.Add("OrganizationId", organizationId);

        if (!string.IsNullOrWhiteSpace(name))
        {
            whereClauses.Add("ig.name ILIKE @Name");
            parameters.Add("Name", $"%{name}%");
        }

        if (isActive.HasValue)
        {
            whereClauses.Add("ig.is_active = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }

        var whereSql = $"WHERE {string.Join(" AND ", whereClauses)}";

        var orderByClause = sortBy?.ToLowerInvariant() switch
        {
            "alphabetical" => "ig.name ASC",
            "recent" => "ig.created_at DESC",
            _ => "ig.created_at DESC"
        };

        using var connection = new NpgsqlConnection(_dbConnectionString);

        var countSql = $"SELECT COUNT(*) FROM interview_guides ig {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT ig.id, ig.group_id, ig.organization_id, ig.name, ig.description, ig.opening_template, ig.closing_template,
                   ig.scoring_rubric, ig.is_active, ig.visibility_scope,
                   ig.created_at, ig.updated_at, ig.created_by, ig.updated_by, ig.is_deleted, ig.deleted_at, ig.deleted_by,
                   (SELECT COUNT(*) FROM interview_guide_questions igq WHERE igq.interview_guide_id = ig.id) as question_count
            FROM interview_guides ig
            {whereSql}
            ORDER BY {orderByClause}
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<InterviewGuide>(querySql, parameters);

        return new PaginatedResult<InterviewGuide>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Searches for inherited interview guides (from ancestor organizations with propagating visibility).
    /// </summary>
    public async Task<PaginatedResult<InterviewGuide>> SearchInherited(
        Guid groupId,
        IReadOnlyList<Guid> ancestorOrgIds,
        string? name,
        bool? isActive,
        string? sortBy,
        int pageNumber,
        int pageSize)
    {
        if (ancestorOrgIds == null || ancestorOrgIds.Count == 0)
        {
            return new PaginatedResult<InterviewGuide>(Enumerable.Empty<InterviewGuide>(), 0, pageNumber, pageSize);
        }

        var whereClauses = new List<string>
        {
            "ig.group_id = @GroupId",
            "ig.organization_id = ANY(@AncestorOrgIds)",
            "ig.visibility_scope IN ('organization_and_descendants', 'descendants_only')",
            "ig.is_deleted = false"
        };
        var parameters = new DynamicParameters();
        parameters.Add("GroupId", groupId);
        parameters.Add("AncestorOrgIds", ancestorOrgIds.ToArray());

        if (!string.IsNullOrWhiteSpace(name))
        {
            whereClauses.Add("ig.name ILIKE @Name");
            parameters.Add("Name", $"%{name}%");
        }

        if (isActive.HasValue)
        {
            whereClauses.Add("ig.is_active = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }

        var whereSql = $"WHERE {string.Join(" AND ", whereClauses)}";

        var orderByClause = sortBy?.ToLowerInvariant() switch
        {
            "alphabetical" => "ig.name ASC",
            "recent" => "ig.created_at DESC",
            _ => "ig.created_at DESC"
        };

        using var connection = new NpgsqlConnection(_dbConnectionString);

        var countSql = $"SELECT COUNT(*) FROM interview_guides ig {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT ig.id, ig.group_id, ig.organization_id, ig.name, ig.description, ig.opening_template, ig.closing_template,
                   ig.scoring_rubric, ig.is_active, ig.visibility_scope,
                   ig.created_at, ig.updated_at, ig.created_by, ig.updated_by, ig.is_deleted, ig.deleted_at, ig.deleted_by,
                   (SELECT COUNT(*) FROM interview_guide_questions igq WHERE igq.interview_guide_id = ig.id) as question_count
            FROM interview_guides ig
            {whereSql}
            ORDER BY {orderByClause}
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<InterviewGuide>(querySql, parameters);

        return new PaginatedResult<InterviewGuide>(items, totalCount, pageNumber, pageSize);
    }

    // Question management methods
    public async Task<InterviewGuideQuestion> AddQuestion(InterviewGuideQuestion question)
    {
        if (question.Id == Guid.Empty)
        {
            question.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO interview_guide_questions (id, interview_guide_id, question, display_order, scoring_weight, scoring_guidance, follow_ups_enabled, max_follow_ups)
            VALUES (@Id, @InterviewGuideId, @Question, @DisplayOrder, @ScoringWeight, @ScoringGuidance, @FollowUpsEnabled, @MaxFollowUps)
            RETURNING id, interview_guide_id, question, display_order, scoring_weight, scoring_guidance, follow_ups_enabled, max_follow_ups, created_at, updated_at";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<InterviewGuideQuestion>(sql, question);
        return newItem!;
    }

    public async Task<InterviewGuideQuestion> UpdateQuestion(InterviewGuideQuestion question)
    {
        const string sql = @"
            UPDATE interview_guide_questions
            SET
                question = @Question,
                display_order = @DisplayOrder,
                scoring_weight = @ScoringWeight,
                scoring_guidance = @ScoringGuidance,
                follow_ups_enabled = @FollowUpsEnabled,
                max_follow_ups = @MaxFollowUps,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id
            RETURNING id, interview_guide_id, question, display_order, scoring_weight, scoring_guidance, follow_ups_enabled, max_follow_ups, created_at, updated_at";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<InterviewGuideQuestion>(sql, question);
        if (updatedItem == null)
        {
            throw new InterviewGuideNotFoundException("Interview guide question not found.");
        }
        return updatedItem;
    }

    public async Task<bool> DeleteQuestion(Guid questionId)
    {
        const string sql = "DELETE FROM interview_guide_questions WHERE id = @questionId";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { questionId });
        return rowsAffected > 0;
    }

    public async Task<InterviewGuideQuestion?> GetQuestionById(Guid questionId)
    {
        const string sql = @"
            SELECT id, interview_guide_id, question, display_order, scoring_weight, scoring_guidance, follow_ups_enabled, max_follow_ups, created_at, updated_at
            FROM interview_guide_questions
            WHERE id = @questionId";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewGuideQuestion>(sql, new { questionId });
    }

    public async Task<List<InterviewGuideQuestion>> GetQuestionsByGuideId(Guid guideId)
    {
        const string sql = @"
            SELECT id, interview_guide_id, question, display_order, scoring_weight, scoring_guidance, follow_ups_enabled, max_follow_ups, created_at, updated_at
            FROM interview_guide_questions
            WHERE interview_guide_id = @guideId
            ORDER BY display_order";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var questions = await connection.QueryAsync<InterviewGuideQuestion>(sql, new { guideId });
        return questions.ToList();
    }

    public async Task ReplaceQuestions(Guid guideId, List<InterviewGuideQuestion> questions)
    {
        using var connection = new NpgsqlConnection(_dbConnectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Get the IDs of questions that will be deleted
            const string getQuestionIdsSql = @"
                SELECT id FROM interview_guide_questions 
                WHERE interview_guide_id = @guideId";
            var questionIdsToDelete = (await connection.QueryAsync<Guid>(getQuestionIdsSql, new { guideId }, transaction)).ToList();

            // Nullify the foreign key in follow_up_templates for questions being deleted
            if (questionIdsToDelete.Count > 0)
            {
                const string nullifyFollowUpsSql = @"
                    UPDATE follow_up_templates 
                    SET interview_guide_question_id = NULL 
                    WHERE interview_guide_question_id = ANY(@questionIds)";
                await connection.ExecuteAsync(nullifyFollowUpsSql, new { questionIds = questionIdsToDelete.ToArray() }, transaction);
            }

            // Delete existing questions
            const string deleteSql = "DELETE FROM interview_guide_questions WHERE interview_guide_id = @guideId";
            await connection.ExecuteAsync(deleteSql, new { guideId }, transaction);

            // Insert new questions
            if (questions.Count > 0)
            {
                const string insertSql = @"
                    INSERT INTO interview_guide_questions (id, interview_guide_id, question, display_order, scoring_weight, scoring_guidance, follow_ups_enabled, max_follow_ups)
                    VALUES (@Id, @InterviewGuideId, @Question, @DisplayOrder, @ScoringWeight, @ScoringGuidance, @FollowUpsEnabled, @MaxFollowUps)";

                foreach (var question in questions)
                {
                    if (question.Id == Guid.Empty)
                    {
                        question.Id = Guid.NewGuid();
                    }
                    question.InterviewGuideId = guideId;
                    await connection.ExecuteAsync(insertSql, question, transaction);
                }
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
