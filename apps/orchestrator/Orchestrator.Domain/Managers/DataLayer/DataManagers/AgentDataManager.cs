using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for Agent entities
/// </summary>
internal sealed class AgentDataManager
{
    private readonly string _dbConnectionString;

    public AgentDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<Agent>();
    }

    public async Task<Agent?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, organization_id, display_name, profile_image_url,
                   system_prompt, interview_guidelines,
                   elevenlabs_voice_id, voice_stability, voice_similarity_boost,
                   voice_provider, voice_type, voice_name,
                   created_at, updated_at, created_by, is_deleted
            FROM agents
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Agent>(sql, new { id });
    }

    public async Task<Agent> Add(Agent agent)
    {
        if (agent.Id == Guid.Empty)
        {
            agent.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO agents (id, organization_id, display_name, profile_image_url,
                system_prompt, interview_guidelines,
                elevenlabs_voice_id, voice_stability, voice_similarity_boost,
                voice_provider, voice_type, voice_name, created_by)
            VALUES (@Id, @OrganizationId, @DisplayName, @ProfileImageUrl,
                @SystemPrompt, @InterviewGuidelines,
                @ElevenlabsVoiceId, @VoiceStability, @VoiceSimilarityBoost,
                @VoiceProvider, @VoiceType, @VoiceName, @CreatedBy)
            RETURNING id, organization_id, display_name, profile_image_url,
                system_prompt, interview_guidelines,
                elevenlabs_voice_id, voice_stability, voice_similarity_boost,
                voice_provider, voice_type, voice_name,
                created_at, updated_at, created_by, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<Agent>(sql, agent);
        return newItem!;
    }

    public async Task<Agent> Update(Agent agent)
    {
        const string sql = @"
            UPDATE agents
            SET
                display_name = @DisplayName,
                profile_image_url = @ProfileImageUrl,
                system_prompt = @SystemPrompt,
                interview_guidelines = @InterviewGuidelines,
                elevenlabs_voice_id = @ElevenlabsVoiceId,
                voice_stability = @VoiceStability,
                voice_similarity_boost = @VoiceSimilarityBoost,
                voice_provider = @VoiceProvider,
                voice_type = @VoiceType,
                voice_name = @VoiceName,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, organization_id, display_name, profile_image_url,
                system_prompt, interview_guidelines,
                elevenlabs_voice_id, voice_stability, voice_similarity_boost,
                voice_provider, voice_type, voice_name,
                created_at, updated_at, created_by, updated_by, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<Agent>(sql, agent);
        if (updatedItem == null)
        {
            throw new AgentNotFoundException("Agent not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE agents
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<Agent>> Search(Guid? organizationId, string? displayName, string? createdBy, string? sortBy, int pageNumber, int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (organizationId.HasValue)
        {
            whereClauses.Add("organization_id = @OrganizationId");
            parameters.Add("OrganizationId", organizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            whereClauses.Add("display_name ILIKE @DisplayName");
            parameters.Add("DisplayName", $"%{displayName}%");
        }

        if (!string.IsNullOrWhiteSpace(createdBy))
        {
            whereClauses.Add("created_by = @CreatedBy");
            parameters.Add("CreatedBy", createdBy);
        }

        whereClauses.Add("is_deleted = false");

        var whereSql = $"WHERE {string.Join(" AND ", whereClauses)}";

        // Determine ORDER BY clause based on sortBy parameter
        var orderByClause = sortBy?.ToLowerInvariant() switch
        {
            "alphabetical" => "display_name ASC",
            "recent" => "created_at DESC",
            _ => "created_at DESC" // Default to most recent
        };

        using var connection = new NpgsqlConnection(_dbConnectionString);
        
        var countSql = $"SELECT COUNT(*) FROM agents {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT id, organization_id, display_name, profile_image_url,
                   system_prompt, interview_guidelines,
                   elevenlabs_voice_id, voice_stability, voice_similarity_boost,
                   voice_provider, voice_type, voice_name,
                   created_at, updated_at, created_by, is_deleted
            FROM agents
            {whereSql}
            ORDER BY {orderByClause}
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<Agent>(querySql, parameters);

        return new PaginatedResult<Agent>(items, totalCount, pageNumber, pageSize);
    }
}
