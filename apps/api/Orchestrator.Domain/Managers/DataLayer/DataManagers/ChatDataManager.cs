using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for Chat entities
/// </summary>
internal sealed class ChatDataManager
{
    private readonly string _dbConnectionString;

    public ChatDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<Chat>();
    }

    public async Task<Chat?> GetById(System.Guid id)
    {
        const string sql = @"
            SELECT id, persona_id, user_id, title, last_message_at, created_at, updated_at, is_deleted
            FROM chats
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Chat>(sql, new { id });
    }

    public async Task<Chat> Add(Chat chat)
    {
        if (chat.Id == System.Guid.Empty)
        {
            chat.Id = System.Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO chats (id, persona_id, user_id, title, last_message_at)
            VALUES (@Id, @PersonaId, @UserId, @Title, @LastMessageAt)
            RETURNING id, persona_id, user_id, title, last_message_at, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<Chat>(sql, chat);
        return newItem!;
    }

    public async Task<Chat> Update(Chat chat)
    {
        const string sql = @"
            UPDATE chats
            SET
                persona_id = @PersonaId,
                user_id = @UserId,
                title = @Title,
                last_message_at = @LastMessageAt,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id AND is_deleted = false
            RETURNING id, persona_id, user_id, title, last_message_at, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<Chat>(sql, chat);
        if (updatedItem == null)
        {
            throw new ChatNotFoundException("Chat not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(System.Guid id)
    {
        const string sql = @"
            UPDATE chats
            SET is_deleted = true, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<Chat>> Search(Guid? personaId, Guid? userId, string? title, int pageNumber, int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (personaId.HasValue)
        {
            whereClauses.Add("persona_id = @PersonaId");
            parameters.Add("PersonaId", personaId.Value);
        }

        if (userId.HasValue)
        {
            whereClauses.Add("user_id = @UserId");
            parameters.Add("UserId", userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            whereClauses.Add("title ILIKE @Title");
            parameters.Add("Title", $"%{title}%");
        }

        whereClauses.Add("is_deleted = false");

        var whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var countSql = $"SELECT COUNT(*) FROM chats {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT id, persona_id, user_id, title, last_message_at, created_at, updated_at, is_deleted
            FROM chats
            {whereSql}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<Chat>(querySql, parameters);

        return new PaginatedResult<Chat>(items, totalCount, pageNumber, pageSize);
    }
}

