using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for AgentCategory entities
/// </summary>
internal sealed class AgentCategoryDataManager
{
    private readonly string _dbConnectionString;

    public AgentCategoryDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<AgentCategory>();
    }

    public async Task<AgentCategory?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, agent_id, category_id, created_at, updated_at
            FROM agent_categories
            WHERE id = @id";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<AgentCategory>(sql, new { id });
    }

    public async Task<AgentCategory> Add(AgentCategory agentCategory)
    {
        if (agentCategory.Id == Guid.Empty)
        {
            agentCategory.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO agent_categories (id, agent_id, category_id)
            VALUES (@Id, @AgentId, @CategoryId)
            RETURNING id, agent_id, category_id, created_at, updated_at";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<AgentCategory>(sql, agentCategory);
        return newItem!;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            DELETE FROM agent_categories
            WHERE id = @id";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByAgentAndCategory(Guid agentId, Guid categoryId)
    {
        const string sql = @"
            DELETE FROM agent_categories
            WHERE agent_id = @agentId AND category_id = @categoryId";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { agentId, categoryId });
        return rowsAffected > 0;
    }

    public async Task<IEnumerable<AgentCategory>> GetByAgentId(Guid agentId)
    {
        const string sql = @"
            SELECT id, agent_id, category_id, created_at, updated_at
            FROM agent_categories
            WHERE agent_id = @agentId
            ORDER BY created_at DESC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<AgentCategory>(sql, new { agentId });
    }

    public async Task<IEnumerable<AgentCategory>> GetByCategoryId(Guid categoryId)
    {
        const string sql = @"
            SELECT id, agent_id, category_id, created_at, updated_at
            FROM agent_categories
            WHERE category_id = @categoryId
            ORDER BY created_at DESC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<AgentCategory>(sql, new { categoryId });
    }

    public async Task<AgentCategory?> GetByAgentAndCategory(Guid agentId, Guid categoryId)
    {
        const string sql = @"
            SELECT id, agent_id, category_id, created_at, updated_at
            FROM agent_categories
            WHERE agent_id = @agentId AND category_id = @categoryId";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<AgentCategory>(sql, new { agentId, categoryId });
    }

    public async Task<IEnumerable<Category>> GetCategoriesByAgentId(Guid agentId)
    {
        const string sql = @"
            SELECT c.id, c.name, c.description, c.category_type, c.display_order, c.is_active, 
                   c.created_by, c.created_at, c.updated_at, c.updated_by, c.is_deleted, c.deleted_at, c.deleted_by
            FROM categories c
            INNER JOIN agent_categories ac ON c.id = ac.category_id
            WHERE ac.agent_id = @agentId AND c.is_deleted = false
            ORDER BY c.display_order ASC, c.name ASC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<Category>(sql, new { agentId });
    }
}

