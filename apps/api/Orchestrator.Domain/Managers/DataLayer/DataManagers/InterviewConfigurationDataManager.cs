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
        DapperConfiguration.ConfigureSnakeCaseMapping<InterviewConfigurationQuestion>();
    }

    public async Task<InterviewConfiguration?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, organization_id, agent_id, name, description, scoring_rubric, is_active,
                   created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by
            FROM interview_configurations
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewConfiguration>(sql, new { id });
    }

    public async Task<InterviewConfiguration?> GetByIdWithQuestions(Guid id)
    {
        const string configSql = @"
            SELECT id, organization_id, agent_id, name, description, scoring_rubric, is_active,
                   created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by
            FROM interview_configurations
            WHERE id = @id AND is_deleted = false";

        const string questionsSql = @"
            SELECT id, interview_configuration_id, question, display_order, scoring_weight, scoring_guidance,
                   follow_ups_enabled, max_follow_ups, created_at, updated_at
            FROM interview_configuration_questions
            WHERE interview_configuration_id = @id
            ORDER BY display_order";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        
        var config = await connection.QueryFirstOrDefaultAsync<InterviewConfiguration>(configSql, new { id });
        if (config == null) return null;

        var questions = await connection.QueryAsync<InterviewConfigurationQuestion>(questionsSql, new { id });
        config.Questions = questions.ToList();

        return config;
    }

    public async Task<InterviewConfiguration> Add(InterviewConfiguration config)
    {
        if (config.Id == Guid.Empty)
        {
            config.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO interview_configurations (id, organization_id, agent_id, name, description, scoring_rubric, is_active, created_by)
            VALUES (@Id, @OrganizationId, @AgentId, @Name, @Description, @ScoringRubric, @IsActive, @CreatedBy)
            RETURNING id, organization_id, agent_id, name, description, scoring_rubric, is_active,
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
                scoring_rubric = @ScoringRubric,
                is_active = @IsActive,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, organization_id, agent_id, name, description, scoring_rubric, is_active,
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
        Guid? organizationId, 
        Guid? agentId, 
        string? name, 
        bool? isActive,
        string? sortBy, 
        int pageNumber, 
        int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (organizationId.HasValue)
        {
            whereClauses.Add("organization_id = @OrganizationId");
            parameters.Add("OrganizationId", organizationId.Value);
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
            SELECT ic.id, ic.organization_id, ic.agent_id, ic.name, ic.description, ic.scoring_rubric, ic.is_active,
                   ic.created_at, ic.updated_at, ic.created_by, ic.updated_by, ic.is_deleted, ic.deleted_at, ic.deleted_by,
                   (SELECT COUNT(*) FROM interview_configuration_questions icq WHERE icq.interview_configuration_id = ic.id) as question_count
            FROM interview_configurations ic
            {whereSql.Replace("organization_id", "ic.organization_id").Replace("agent_id", "ic.agent_id").Replace("name", "ic.name").Replace("is_active", "ic.is_active").Replace("is_deleted", "ic.is_deleted")}
            ORDER BY {orderByClause.Replace("name", "ic.name").Replace("created_at", "ic.created_at")}
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<InterviewConfiguration>(querySql, parameters);

        return new PaginatedResult<InterviewConfiguration>(items, totalCount, pageNumber, pageSize);
    }

    // Question management methods
    public async Task<InterviewConfigurationQuestion> AddQuestion(InterviewConfigurationQuestion question)
    {
        if (question.Id == Guid.Empty)
        {
            question.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO interview_configuration_questions (id, interview_configuration_id, question, display_order, scoring_weight, scoring_guidance, follow_ups_enabled, max_follow_ups)
            VALUES (@Id, @InterviewConfigurationId, @Question, @DisplayOrder, @ScoringWeight, @ScoringGuidance, @FollowUpsEnabled, @MaxFollowUps)
            RETURNING id, interview_configuration_id, question, display_order, scoring_weight, scoring_guidance, follow_ups_enabled, max_follow_ups, created_at, updated_at";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<InterviewConfigurationQuestion>(sql, question);
        return newItem!;
    }

    public async Task<InterviewConfigurationQuestion> UpdateQuestion(InterviewConfigurationQuestion question)
    {
        const string sql = @"
            UPDATE interview_configuration_questions
            SET
                question = @Question,
                display_order = @DisplayOrder,
                scoring_weight = @ScoringWeight,
                scoring_guidance = @ScoringGuidance,
                follow_ups_enabled = @FollowUpsEnabled,
                max_follow_ups = @MaxFollowUps,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id
            RETURNING id, interview_configuration_id, question, display_order, scoring_weight, scoring_guidance, follow_ups_enabled, max_follow_ups, created_at, updated_at";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<InterviewConfigurationQuestion>(sql, question);
        if (updatedItem == null)
        {
            throw new InterviewConfigurationNotFoundException("Interview configuration question not found.");
        }
        return updatedItem;
    }

    public async Task<bool> DeleteQuestion(Guid questionId)
    {
        const string sql = "DELETE FROM interview_configuration_questions WHERE id = @questionId";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { questionId });
        return rowsAffected > 0;
    }

    public async Task<InterviewConfigurationQuestion?> GetQuestionById(Guid questionId)
    {
        const string sql = @"
            SELECT id, interview_configuration_id, question, display_order, scoring_weight, scoring_guidance, follow_ups_enabled, max_follow_ups, created_at, updated_at
            FROM interview_configuration_questions
            WHERE id = @questionId";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewConfigurationQuestion>(sql, new { questionId });
    }

    public async Task<List<InterviewConfigurationQuestion>> GetQuestionsByConfigurationId(Guid configurationId)
    {
        const string sql = @"
            SELECT id, interview_configuration_id, question, display_order, scoring_weight, scoring_guidance, follow_ups_enabled, max_follow_ups, created_at, updated_at
            FROM interview_configuration_questions
            WHERE interview_configuration_id = @configurationId
            ORDER BY display_order";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var questions = await connection.QueryAsync<InterviewConfigurationQuestion>(sql, new { configurationId });
        return questions.ToList();
    }

    public async Task ReplaceQuestions(Guid configurationId, List<InterviewConfigurationQuestion> questions)
    {
        using var connection = new NpgsqlConnection(_dbConnectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // First, get the IDs of questions that will be deleted
            const string getQuestionIdsSql = @"
                SELECT id FROM interview_configuration_questions 
                WHERE interview_configuration_id = @configurationId";
            var questionIdsToDelete = (await connection.QueryAsync<Guid>(getQuestionIdsSql, new { configurationId }, transaction)).ToList();

            // Nullify the foreign key in follow_up_templates for questions being deleted
            if (questionIdsToDelete.Count > 0)
            {
                const string nullifyFollowUpsSql = @"
                    UPDATE follow_up_templates 
                    SET interview_configuration_question_id = NULL 
                    WHERE interview_configuration_question_id = ANY(@questionIds)";
                await connection.ExecuteAsync(nullifyFollowUpsSql, new { questionIds = questionIdsToDelete.ToArray() }, transaction);
            }

            // Delete existing questions
            const string deleteSql = "DELETE FROM interview_configuration_questions WHERE interview_configuration_id = @configurationId";
            await connection.ExecuteAsync(deleteSql, new { configurationId }, transaction);

            // Insert new questions
            if (questions.Count > 0)
            {
                const string insertSql = @"
                    INSERT INTO interview_configuration_questions (id, interview_configuration_id, question, display_order, scoring_weight, scoring_guidance, follow_ups_enabled, max_follow_ups)
                    VALUES (@Id, @InterviewConfigurationId, @Question, @DisplayOrder, @ScoringWeight, @ScoringGuidance, @FollowUpsEnabled, @MaxFollowUps)";

                foreach (var question in questions)
                {
                    if (question.Id == Guid.Empty)
                    {
                        question.Id = Guid.NewGuid();
                    }
                    question.InterviewConfigurationId = configurationId;
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
