using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for Topic entities
/// </summary>
internal sealed class TopicDataManager
{
    private readonly string _dbConnectionString;

    public TopicDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<Topic>();
        DapperConfiguration.ConfigureSnakeCaseMapping<TopicFeedData>();
    }

    public async Task<Topic?> GetById(System.Guid id)
    {
        const string sql = @"
            SELECT id, name, description, category_id, persona_id, content_url, contribution_notes, created_by, created_at, updated_at, created_by, updated_by, is_deleted
            FROM topics
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Topic>(sql, new { id });
    }

    public async Task<Topic> Add(Topic topic)
    {
        if (topic.Id == System.Guid.Empty)
        {
            topic.Id = System.Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO topics (id, name, description, category_id, persona_id, content_url, contribution_notes, created_by)
            VALUES (@Id, @Name, @Description, @CategoryId, @PersonaId, @ContentUrl, @ContributionNotes, @CreatedBy)
            RETURNING id, name, description, category_id, persona_id, content_url, contribution_notes, created_by, created_at, updated_at, created_by, updated_by, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<Topic>(sql, topic);
        return newItem!;
    }

    public async Task<Topic> Update(Topic topic)
    {
        const string sql = @"
            UPDATE topics
            SET
                name = @Name,
                description = @Description,
                category_id = @CategoryId,
                persona_id = @PersonaId,
                content_url = @ContentUrl,
                contribution_notes = @ContributionNotes,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, name, description, category_id, persona_id, content_url, contribution_notes, created_by, created_at, updated_at, created_by, updated_by, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<Topic>(sql, topic);
        if (updatedItem == null)
        {
            throw new TopicNotFoundException("Topic not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(System.Guid id)
    {
        const string sql = @"
            UPDATE topics
            SET is_deleted = true, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<Topic>> Search(string? name, Guid? personaId, int pageNumber, int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(name))
        {
            whereClauses.Add("name ILIKE @Name");
            parameters.Add("Name", $"%{name}%");
        }

        if (personaId.HasValue)
        {
            whereClauses.Add("persona_id = @PersonaId");
            parameters.Add("PersonaId", personaId.Value);
        }

        whereClauses.Add("is_deleted = false");

        var whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var countSql = $"SELECT COUNT(*) FROM topics {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT id, name, description, category_id, persona_id, content_url, contribution_notes, created_by, created_at, updated_at, created_by, updated_by, is_deleted
            FROM topics
            {whereSql}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<Topic>(querySql, parameters);

        return new PaginatedResult<Topic>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Adds a tag to a topic (creates topic_tags junction entry)
    /// </summary>
    public async Task AddTopicTag(Guid topicId, Guid tagId)
    {
        const string sql = @"
            INSERT INTO topic_tags (topic_id, tag_id)
            VALUES (@topicId, @tagId)
            ON CONFLICT (topic_id, tag_id) DO NOTHING";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        await connection.ExecuteAsync(sql, new { topicId, tagId });
    }

    /// <summary>
    /// Removes a tag from a topic (deletes topic_tags junction entry)
    /// </summary>
    public async Task<bool> RemoveTopicTag(Guid topicId, Guid tagId)
    {
        const string sql = @"
            DELETE FROM topic_tags
            WHERE topic_id = @topicId AND tag_id = @tagId";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { topicId, tagId });
        return rowsAffected > 0;
    }

    /// <summary>
    /// Deletes all tags for a topic
    /// </summary>
    public async Task DeleteAllTopicTags(Guid topicId)
    {
        const string sql = @"
            DELETE FROM topic_tags
            WHERE topic_id = @topicId";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        await connection.ExecuteAsync(sql, new { topicId });
    }

    /// <summary>
    /// Searches for topics with enriched feed data including author info and engagement metrics
    /// </summary>
    public async Task<PaginatedResult<TopicFeedData>> SearchFeed(
        Guid? categoryId, 
        Guid[]? tagIds, 
        string? searchTerm,
        string? sortBy,
        int pageNumber, 
        int pageSize)
    {
        var whereClauses = new List<string> { "t.is_deleted = false", "p.is_deleted = false" };
        var parameters = new DynamicParameters();

        if (categoryId.HasValue)
        {
            whereClauses.Add("t.category_id = @CategoryId");
            parameters.Add("CategoryId", categoryId.Value);
        }

        // If tagIds are provided, add a subquery to filter topics that have at least one of these tags
        if (tagIds != null && tagIds.Length > 0)
        {
            whereClauses.Add("t.id IN (SELECT topic_id FROM topic_tags WHERE tag_id = ANY(@TagIds))");
            parameters.Add("TagIds", tagIds);
        }

        // Add search term filtering for name and description
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereClauses.Add("(t.name ILIKE @SearchTerm OR t.description ILIKE @SearchTerm)");
            parameters.Add("SearchTerm", $"%{searchTerm}%");
        }

        var whereSql = string.Join(" AND ", whereClauses);

        // Determine ORDER BY clause based on sortBy parameter
        var orderByClause = sortBy?.ToLowerInvariant() switch
        {
            "chat_count" => "chat_count DESC, t.created_at DESC",
            "recent" => "t.created_at DESC",
            "popular" => "chat_count DESC, t.created_at DESC", // Default to chat count as popularity
            _ => "t.created_at DESC" // Default to most recent
        };

        using var connection = new NpgsqlConnection(_dbConnectionString);

        // Count total matching topics (must include same JOINs as main query for WHERE clause)
        var countSql = $@"
            SELECT COUNT(DISTINCT t.id)
            FROM topics t
            INNER JOIN personas p ON t.persona_id = p.id
            WHERE {whereSql}";

        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        // Get feed data with JOINs
        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT 
                t.id,
                t.name,
                t.description,
                t.persona_id,
                t.category_id,
                c.name as category_name,
                p.id as author_id,
                p.first_name as author_first_name,
                p.last_name as author_last_name,
                p.profile_image_url as author_profile_image_url,
                COALESCE(chat_counts.chat_count, 0) as chat_count,
                t.created_at,
                t.updated_at
            FROM topics t
            INNER JOIN categories c ON t.category_id = c.id
            INNER JOIN personas p ON t.persona_id = p.id
            LEFT JOIN (
                SELECT topic_id, COUNT(DISTINCT chat_id) as chat_count
                FROM chat_topics
                GROUP BY topic_id
            ) chat_counts ON t.id = chat_counts.topic_id
            WHERE {whereSql}
            ORDER BY {orderByClause}
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<TopicFeedData>(querySql, parameters);

        return new PaginatedResult<TopicFeedData>(items, totalCount, pageNumber, pageSize);
    }
}

