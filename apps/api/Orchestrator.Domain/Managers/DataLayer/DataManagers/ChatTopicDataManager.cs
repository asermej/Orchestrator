using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for ChatTopic entities
/// </summary>
internal sealed class ChatTopicDataManager
{
    private readonly string _dbConnectionString;

    public ChatTopicDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<ChatTopic>();
    }

    public async Task<ChatTopic?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, chat_id, topic_id, added_at
            FROM chat_topics
            WHERE id = @id";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<ChatTopic>(sql, new { id });
    }

    public async Task<ChatTopic?> GetByChatAndTopic(Guid chatId, Guid topicId)
    {
        const string sql = @"
            SELECT id, chat_id, topic_id, added_at
            FROM chat_topics
            WHERE chat_id = @chatId AND topic_id = @topicId";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<ChatTopic>(sql, new { chatId, topicId });
    }

    public async Task<ChatTopic> Add(ChatTopic chatTopic)
    {
        if (chatTopic.Id == Guid.Empty)
        {
            chatTopic.Id = Guid.NewGuid();
        }

        if (chatTopic.AddedAt == DateTime.MinValue)
        {
            chatTopic.AddedAt = DateTime.UtcNow;
        }

        const string sql = @"
            INSERT INTO chat_topics (id, chat_id, topic_id, added_at)
            VALUES (@Id, @ChatId, @TopicId, @AddedAt)
            RETURNING id, chat_id, topic_id, added_at";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<ChatTopic>(sql, chatTopic);
        return newItem!;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = "DELETE FROM chat_topics WHERE id = @id";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByChatAndTopic(Guid chatId, Guid topicId)
    {
        const string sql = "DELETE FROM chat_topics WHERE chat_id = @chatId AND topic_id = @topicId";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { chatId, topicId });
        return rowsAffected > 0;
    }

    public async Task<IEnumerable<ChatTopic>> GetByChat(Guid chatId)
    {
        const string sql = @"
            SELECT id, chat_id, topic_id, added_at
            FROM chat_topics
            WHERE chat_id = @chatId
            ORDER BY added_at DESC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<ChatTopic>(sql, new { chatId });
    }
}

