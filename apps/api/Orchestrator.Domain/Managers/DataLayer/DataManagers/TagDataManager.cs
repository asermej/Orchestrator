using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for Tag entities
/// </summary>
internal sealed class TagDataManager
{
    private readonly string _dbConnectionString;

    public TagDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<Tag>();
    }

    public async Task<Tag?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, name, created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by
            FROM tags
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Tag>(sql, new { id });
    }

    public async Task<Tag?> GetByName(string name)
    {
        // Normalize name to lowercase for case-insensitive lookup
        var normalizedName = name.Trim().ToLowerInvariant();
        
        const string sql = @"
            SELECT id, name, created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by
            FROM tags
            WHERE name = @name AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Tag>(sql, new { name = normalizedName });
    }

    public async Task<Tag> Add(Tag tag)
    {
        if (tag.Id == Guid.Empty)
        {
            tag.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO tags (id, name, created_by)
            VALUES (@Id, @Name, @CreatedBy)
            RETURNING id, name, created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<Tag>(sql, tag);
        return newItem!;
    }

    public async Task<Tag> GetOrCreate(string name, string? createdBy = null)
    {
        // Normalize name to lowercase
        var normalizedName = name.Trim().ToLowerInvariant();
        
        // Try to get existing tag
        var existingTag = await GetByName(normalizedName);
        if (existingTag != null)
        {
            return existingTag;
        }

        // Create new tag if it doesn't exist
        var newTag = new Tag
        {
            Name = normalizedName,
            CreatedBy = createdBy
        };

        return await Add(newTag);
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE tags
            SET is_deleted = true, 
                deleted_at = CURRENT_TIMESTAMP,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<Tag>> Search(string? searchTerm, int pageNumber, int pageSize)
    {
        var parameters = new DynamicParameters();
        var whereClauses = new List<string> { "t.is_deleted = false" };

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereClauses.Add("t.name ILIKE @SearchTerm");
            parameters.Add("SearchTerm", $"%{searchTerm.Trim().ToLowerInvariant()}%");
        }

        var whereSql = $"WHERE {string.Join(" AND ", whereClauses)}";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        
        // Count only tags that are in use (have at least one topic_tags association)
        var countSql = $@"
            SELECT COUNT(DISTINCT t.id)
            FROM tags t
            INNER JOIN topic_tags tt ON t.id = tt.tag_id
            {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        
        // Select only tags that are in use (have at least one topic_tags association)
        var querySql = $@"
            SELECT DISTINCT t.id, t.name, t.created_at, t.updated_at, t.created_by, t.updated_by, t.is_deleted, t.deleted_at, t.deleted_by
            FROM tags t
            INNER JOIN topic_tags tt ON t.id = tt.tag_id
            {whereSql}
            ORDER BY t.name ASC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<Tag>(querySql, parameters);

        return new PaginatedResult<Tag>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IEnumerable<Tag>> GetTagsByTopicId(Guid topicId)
    {
        const string sql = @"
            SELECT t.id, t.name, t.created_at, t.updated_at, t.created_by, t.updated_by, t.is_deleted, t.deleted_at, t.deleted_by
            FROM tags t
            INNER JOIN topic_tags tt ON t.id = tt.tag_id
            WHERE tt.topic_id = @topicId AND t.is_deleted = false
            ORDER BY t.name ASC";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<Tag>(sql, new { topicId });
    }

    /// <summary>
    /// Gets tags for multiple topics in a single query (batch operation to avoid N+1 problem)
    /// </summary>
    /// <param name="topicIds">Array of topic IDs to fetch tags for</param>
    /// <returns>Dictionary mapping topic ID to its list of tags</returns>
    public async Task<Dictionary<Guid, List<Tag>>> GetTagsByTopicIds(Guid[] topicIds)
    {
        if (topicIds == null || topicIds.Length == 0)
        {
            return new Dictionary<Guid, List<Tag>>();
        }

        const string sql = @"
            SELECT tt.topic_id, t.id, t.name, t.created_at, t.updated_at, t.created_by, t.updated_by, t.is_deleted, t.deleted_at, t.deleted_by
            FROM tags t
            INNER JOIN topic_tags tt ON t.id = tt.tag_id
            WHERE tt.topic_id = ANY(@topicIds) AND t.is_deleted = false
            ORDER BY tt.topic_id, t.name ASC";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var results = await connection.QueryAsync<(Guid TopicId, Guid Id, string Name, DateTime CreatedAt, DateTime UpdatedAt, string? CreatedBy, string? UpdatedBy, bool IsDeleted, DateTime? DeletedAt, string? DeletedBy)>(sql, new { topicIds });

        // Group tags by topic ID
        var tagsByTopicId = new Dictionary<Guid, List<Tag>>();
        foreach (var result in results)
        {
            if (!tagsByTopicId.ContainsKey(result.TopicId))
            {
                tagsByTopicId[result.TopicId] = new List<Tag>();
            }

            tagsByTopicId[result.TopicId].Add(new Tag
            {
                Id = result.Id,
                Name = result.Name,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt,
                CreatedBy = result.CreatedBy,
                UpdatedBy = result.UpdatedBy,
                IsDeleted = result.IsDeleted,
                DeletedAt = result.DeletedAt,
                DeletedBy = result.DeletedBy
            });
        }

        return tagsByTopicId;
    }
}

