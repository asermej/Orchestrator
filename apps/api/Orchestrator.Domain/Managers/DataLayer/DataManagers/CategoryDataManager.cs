using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for Category entities
/// </summary>
internal sealed class CategoryDataManager
{
    private readonly string _dbConnectionString;

    public CategoryDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<Category>();
    }

    public async Task<Category?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, name, description, category_type, display_order, is_active, 
                   created_by, created_at, updated_at, updated_by, is_deleted, deleted_at, deleted_by
            FROM categories
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { id });
    }

    public async Task<Category> Add(Category category)
    {
        if (category.Id == Guid.Empty)
        {
            category.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO categories (id, name, description, category_type, display_order, is_active, created_by)
            VALUES (@Id, @Name, @Description, @CategoryType, @DisplayOrder, @IsActive, @CreatedBy)
            RETURNING id, name, description, category_type, display_order, is_active, 
                      created_by, created_at, updated_at, updated_by, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<Category>(sql, category);
        return newItem!;
    }

    public async Task<Category> Update(Category category)
    {
        const string sql = @"
            UPDATE categories
            SET
                name = @Name,
                description = @Description,
                category_type = @CategoryType,
                display_order = @DisplayOrder,
                is_active = @IsActive,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, name, description, category_type, display_order, is_active, 
                      created_by, created_at, updated_at, updated_by, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<Category>(sql, category);
        if (updatedItem == null)
        {
            throw new CategoryNotFoundException("Category not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE categories
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<Category>> Search(string? name, string? categoryType, bool? isActive, int pageNumber, int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(name))
        {
            whereClauses.Add("name ILIKE @Name");
            parameters.Add("Name", $"%{name}%");
        }

        if (!string.IsNullOrWhiteSpace(categoryType))
        {
            whereClauses.Add("category_type = @CategoryType");
            parameters.Add("CategoryType", categoryType);
        }

        if (isActive.HasValue)
        {
            whereClauses.Add("is_active = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }

        whereClauses.Add("is_deleted = false");

        var whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var countSql = $"SELECT COUNT(*) FROM categories {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT id, name, description, category_type, display_order, is_active, 
                   created_by, created_at, updated_at, updated_by, is_deleted, deleted_at, deleted_by
            FROM categories
            {whereSql}
            ORDER BY display_order ASC, name ASC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<Category>(querySql, parameters);

        return new PaginatedResult<Category>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<Category?> GetByName(string name)
    {
        const string sql = @"
            SELECT id, name, description, category_type, display_order, is_active, 
                   created_by, created_at, updated_at, updated_by, is_deleted, deleted_at, deleted_by
            FROM categories
            WHERE name = @name AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { name });
    }
}

