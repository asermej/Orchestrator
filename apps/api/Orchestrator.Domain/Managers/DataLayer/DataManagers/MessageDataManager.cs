using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for Message entities
/// </summary>
internal sealed class MessageDataManager
{
    private readonly string _dbConnectionString;

    public MessageDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<Message>();
    }

    public async Task<Message?> GetById(System.Guid id)
    {
        const string sql = @"
            SELECT id, chat_id, role, content, created_at, updated_at, is_deleted
            FROM messages
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Message>(sql, new { id });
    }

    public async Task<Message> Add(Message message)
    {
        if (message.Id == System.Guid.Empty)
        {
            message.Id = System.Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO messages (id, chat_id, role, content)
            VALUES (@Id, @ChatId, @Role, @Content)
            RETURNING id, chat_id, role, content, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<Message>(sql, message);
        return newItem!;
    }

    public async Task<bool> Delete(System.Guid id)
    {
        const string sql = @"
            UPDATE messages
            SET is_deleted = true, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<Message>> Search(Guid? chatId, string? role, string? content, int pageNumber, int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (chatId.HasValue)
        {
            whereClauses.Add("chat_id = @ChatId");
            parameters.Add("ChatId", chatId.Value);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            whereClauses.Add("role = @Role");
            parameters.Add("Role", role);
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            whereClauses.Add("content ILIKE @Content");
            parameters.Add("Content", $"%{content}%");
        }

        whereClauses.Add("is_deleted = false");

        var whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var countSql = $"SELECT COUNT(*) FROM messages {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT id, chat_id, role, content, created_at, updated_at, is_deleted
            FROM messages
            {whereSql}
            ORDER BY created_at ASC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<Message>(querySql, parameters);

        return new PaginatedResult<Message>(items, totalCount, pageNumber, pageSize);
    }
}

