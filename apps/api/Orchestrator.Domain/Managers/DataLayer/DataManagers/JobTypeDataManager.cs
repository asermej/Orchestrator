using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for JobType entities
/// </summary>
internal sealed class JobTypeDataManager
{
    private readonly string _dbConnectionString;

    public JobTypeDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<JobType>();
    }

    public async Task<JobType?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, organization_id, name, description, is_active, created_at, updated_at, is_deleted
            FROM job_types
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<JobType>(sql, new { id });
    }

    public async Task<JobType> Add(JobType jobType)
    {
        if (jobType.Id == Guid.Empty)
        {
            jobType.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO job_types (id, organization_id, name, description, is_active, created_by)
            VALUES (@Id, @OrganizationId, @Name, @Description, @IsActive, @CreatedBy)
            RETURNING id, organization_id, name, description, is_active, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<JobType>(sql, jobType);
        return newItem!;
    }

    public async Task<JobType> Update(JobType jobType)
    {
        const string sql = @"
            UPDATE job_types
            SET
                name = @Name,
                description = @Description,
                is_active = @IsActive,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, organization_id, name, description, is_active, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<JobType>(sql, jobType);
        if (updatedItem == null)
        {
            throw new JobTypeNotFoundException("Job type not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE job_types
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<JobType>> Search(Guid? organizationId, string? name, bool? isActive, int pageNumber, int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (organizationId.HasValue)
        {
            whereClauses.Add("organization_id = @OrganizationId");
            parameters.Add("OrganizationId", organizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            whereClauses.Add("name ILIKE @Name");
            parameters.Add("Name", $"%{name}%");
        }

        if (isActive.HasValue)
        {
            whereClauses.Add("is_active = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }

        whereClauses.Add("is_deleted = false");

        var whereSql = $"WHERE {string.Join(" AND ", whereClauses)}";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var countSql = $"SELECT COUNT(*) FROM job_types {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT id, organization_id, name, description, is_active, created_at, updated_at, is_deleted
            FROM job_types
            {whereSql}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<JobType>(querySql, parameters);

        return new PaginatedResult<JobType>(items, totalCount, pageNumber, pageSize);
    }
}
