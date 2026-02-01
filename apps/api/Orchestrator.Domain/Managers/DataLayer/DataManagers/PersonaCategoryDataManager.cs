using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for PersonaCategory entities
/// </summary>
internal sealed class PersonaCategoryDataManager
{
    private readonly string _dbConnectionString;

    public PersonaCategoryDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<PersonaCategory>();
    }

    public async Task<PersonaCategory?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, persona_id, category_id, created_at, updated_at
            FROM persona_categories
            WHERE id = @id";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<PersonaCategory>(sql, new { id });
    }

    public async Task<PersonaCategory> Add(PersonaCategory personaCategory)
    {
        if (personaCategory.Id == Guid.Empty)
        {
            personaCategory.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO persona_categories (id, persona_id, category_id)
            VALUES (@Id, @PersonaId, @CategoryId)
            RETURNING id, persona_id, category_id, created_at, updated_at";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<PersonaCategory>(sql, personaCategory);
        return newItem!;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            DELETE FROM persona_categories
            WHERE id = @id";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByPersonaAndCategory(Guid personaId, Guid categoryId)
    {
        const string sql = @"
            DELETE FROM persona_categories
            WHERE persona_id = @personaId AND category_id = @categoryId";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { personaId, categoryId });
        return rowsAffected > 0;
    }

    public async Task<IEnumerable<PersonaCategory>> GetByPersonaId(Guid personaId)
    {
        const string sql = @"
            SELECT id, persona_id, category_id, created_at, updated_at
            FROM persona_categories
            WHERE persona_id = @personaId
            ORDER BY created_at DESC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<PersonaCategory>(sql, new { personaId });
    }

    public async Task<IEnumerable<PersonaCategory>> GetByCategoryId(Guid categoryId)
    {
        const string sql = @"
            SELECT id, persona_id, category_id, created_at, updated_at
            FROM persona_categories
            WHERE category_id = @categoryId
            ORDER BY created_at DESC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<PersonaCategory>(sql, new { categoryId });
    }

    public async Task<PersonaCategory?> GetByPersonaAndCategory(Guid personaId, Guid categoryId)
    {
        const string sql = @"
            SELECT id, persona_id, category_id, created_at, updated_at
            FROM persona_categories
            WHERE persona_id = @personaId AND category_id = @categoryId";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<PersonaCategory>(sql, new { personaId, categoryId });
    }

    public async Task<IEnumerable<Category>> GetCategoriesByPersonaId(Guid personaId)
    {
        const string sql = @"
            SELECT c.id, c.name, c.description, c.category_type, c.display_order, c.is_active, 
                   c.created_by, c.created_at, c.updated_at, c.updated_by, c.is_deleted, c.deleted_at, c.deleted_by
            FROM categories c
            INNER JOIN persona_categories pc ON c.id = pc.category_id
            WHERE pc.persona_id = @personaId AND c.is_deleted = false
            ORDER BY c.display_order ASC, c.name ASC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<Category>(sql, new { personaId });
    }
}

