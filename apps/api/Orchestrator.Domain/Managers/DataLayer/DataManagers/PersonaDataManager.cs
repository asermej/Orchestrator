using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for Persona entities
/// </summary>
internal sealed class PersonaDataManager
{
    private readonly string _dbConnectionString;

    public PersonaDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<Persona>();
    }

    public async Task<Persona?> GetById(System.Guid id)
    {
        const string sql = @"
            SELECT id, first_name, last_name, display_name, profile_image_url, training_file_path,
                   elevenlabs_voice_id AS eleven_labs_voice_id, voice_stability, voice_similarity_boost,
                   voice_provider, voice_type, voice_name, voice_created_at, voice_created_by_user_id,
                   created_at, updated_at, created_by, is_deleted
            FROM personas
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Persona>(sql, new { id });
    }

    public async Task<Persona> Add(Persona persona)
    {
        if (persona.Id == System.Guid.Empty)
        {
            persona.Id = System.Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO personas (id, first_name, last_name, display_name, profile_image_url, training_file_path,
                elevenlabs_voice_id, voice_stability, voice_similarity_boost,
                voice_provider, voice_type, voice_name, voice_created_at, voice_created_by_user_id, created_by)
            VALUES (@Id, @FirstName, @LastName, @DisplayName, @ProfileImageUrl, @TrainingFilePath,
                @ElevenLabsVoiceId, @VoiceStability, @VoiceSimilarityBoost,
                @VoiceProvider, @VoiceType, @VoiceName, @VoiceCreatedAt, @VoiceCreatedByUserId, @CreatedBy)
            RETURNING id, first_name, last_name, display_name, profile_image_url, training_file_path,
                elevenlabs_voice_id AS eleven_labs_voice_id, voice_stability, voice_similarity_boost,
                voice_provider, voice_type, voice_name, voice_created_at, voice_created_by_user_id,
                created_at, updated_at, created_by, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<Persona>(sql, persona);
        return newItem!;
    }

    public async Task<Persona> Update(Persona persona)
    {
        const string sql = @"
            UPDATE personas
            SET
                first_name = @FirstName,
                last_name = @LastName,
                display_name = @DisplayName,
                profile_image_url = @ProfileImageUrl,
                training_file_path = @TrainingFilePath,
                elevenlabs_voice_id = @ElevenLabsVoiceId,
                voice_stability = @VoiceStability,
                voice_similarity_boost = @VoiceSimilarityBoost,
                voice_provider = @VoiceProvider,
                voice_type = @VoiceType,
                voice_name = @VoiceName,
                voice_created_at = @VoiceCreatedAt,
                voice_created_by_user_id = @VoiceCreatedByUserId,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, first_name, last_name, display_name, profile_image_url, training_file_path,
                elevenlabs_voice_id AS eleven_labs_voice_id, voice_stability, voice_similarity_boost,
                voice_provider, voice_type, voice_name, voice_created_at, voice_created_by_user_id,
                created_at, updated_at, created_by, updated_by, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<Persona>(sql, persona);
        if (updatedItem == null)
        {
            throw new PersonaNotFoundException("Persona not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(System.Guid id)
    {
        const string sql = @"
            UPDATE personas
            SET is_deleted = true, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<Persona>> Search(string? firstName, string? lastName, string? displayName, string? createdBy, Guid? categoryId, string? sortBy, int pageNumber, int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            whereClauses.Add("p.first_name ILIKE @FirstName");
            parameters.Add("FirstName", $"%{firstName}%");
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            whereClauses.Add("p.last_name ILIKE @LastName");
            parameters.Add("LastName", $"%{lastName}%");
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            whereClauses.Add("p.display_name ILIKE @DisplayName");
            parameters.Add("DisplayName", $"%{displayName}%");
        }

        if (!string.IsNullOrWhiteSpace(createdBy))
        {
            whereClauses.Add("p.created_by = @CreatedBy");
            parameters.Add("CreatedBy", createdBy);
        }

        // Category filter: persona must be in the specified category
        if (categoryId.HasValue)
        {
            whereClauses.Add("p.id IN (SELECT persona_id FROM persona_categories WHERE category_id = @CategoryId)");
            parameters.Add("CategoryId", categoryId.Value);
        }

        whereClauses.Add("p.is_deleted = false");

        var whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

        // Determine ORDER BY clause based on sortBy parameter
        var orderByClause = sortBy?.ToLowerInvariant() switch
        {
            "popularity" => "chat_count DESC, p.created_at DESC",
            "alphabetical" => "p.display_name ASC",
            "recent" => "p.created_at DESC",
            _ => "p.created_at DESC" // Default to most recent
        };

        using var connection = new NpgsqlConnection(_dbConnectionString);
        
        // Count query needs same JOINs if we're ordering by chat_count
        var countSql = $@"
            SELECT COUNT(DISTINCT p.id)
            FROM personas p
            {whereSql}";
        
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT 
                p.id, 
                p.first_name, 
                p.last_name, 
                p.display_name, 
                p.profile_image_url, 
                p.training_file_path,
                p.elevenlabs_voice_id AS eleven_labs_voice_id, p.voice_stability, p.voice_similarity_boost,
                p.voice_provider, p.voice_type, p.voice_name, p.voice_created_at, p.voice_created_by_user_id,
                p.created_at, 
                p.updated_at, 
                p.created_by, 
                p.is_deleted,
                COALESCE(chat_counts.chat_count, 0) as chat_count
            FROM personas p
            LEFT JOIN (
                SELECT persona_id, COUNT(DISTINCT id) as chat_count
                FROM chats
                WHERE is_deleted = false
                GROUP BY persona_id
            ) chat_counts ON p.id = chat_counts.persona_id
            {whereSql}
            ORDER BY {orderByClause}
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<Persona>(querySql, parameters);

        return new PaginatedResult<Persona>(items, totalCount, pageNumber, pageSize);
    }
}

