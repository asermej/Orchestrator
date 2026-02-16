using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for Job entities
/// </summary>
internal sealed class JobDataManager
{
    private readonly string _dbConnectionString;

    public JobDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<Job>();
    }

    public async Task<Job?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, group_id, organization_id, external_job_id, title, description, status, location, created_at, updated_at, is_deleted
            FROM jobs
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Job>(sql, new { id });
    }

    public async Task<Job?> GetByExternalId(Guid groupId, string externalJobId)
    {
        const string sql = @"
            SELECT id, group_id, organization_id, external_job_id, title, description, status, location, created_at, updated_at, is_deleted
            FROM jobs
            WHERE group_id = @GroupId AND external_job_id = @ExternalJobId AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Job>(sql, new { GroupId = groupId, ExternalJobId = externalJobId });
    }

    public async Task<Job> Add(Job job)
    {
        if (job.Id == Guid.Empty)
        {
            job.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO jobs (id, group_id, organization_id, external_job_id, title, description, status, location, created_by)
            VALUES (@Id, @GroupId, @OrganizationId, @ExternalJobId, @Title, @Description, @Status, @Location, @CreatedBy)
            RETURNING id, group_id, organization_id, external_job_id, title, description, status, location, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<Job>(sql, job);
        return newItem!;
    }

    public async Task<Job> Update(Job job)
    {
        const string sql = @"
            UPDATE jobs
            SET
                title = @Title,
                description = @Description,
                status = @Status,
                location = @Location,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, group_id, organization_id, external_job_id, title, description, status, location, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<Job>(sql, job);
        if (updatedItem == null)
        {
            throw new JobNotFoundException("Job not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE jobs
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<Job>> Search(Guid? groupId, string? title, string? status, int pageNumber, int pageSize, IReadOnlyList<Guid>? organizationIds = null)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (groupId.HasValue)
        {
            whereClauses.Add("group_id = @GroupId");
            parameters.Add("GroupId", groupId.Value);
        }

        if (organizationIds != null)
        {
            whereClauses.Add("(organization_id IS NULL OR organization_id = ANY(@OrganizationIds))");
            parameters.Add("OrganizationIds", organizationIds.ToArray());
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            whereClauses.Add("title ILIKE @Title");
            parameters.Add("Title", $"%{title}%");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereClauses.Add("status = @Status");
            parameters.Add("Status", status);
        }

        whereClauses.Add("is_deleted = false");

        var whereSql = $"WHERE {string.Join(" AND ", whereClauses)}";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var countSql = $"SELECT COUNT(*) FROM jobs {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT id, group_id, organization_id, external_job_id, title, description, status, location, created_at, updated_at, is_deleted
            FROM jobs
            {whereSql}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<Job>(querySql, parameters);

        return new PaginatedResult<Job>(items, totalCount, pageNumber, pageSize);
    }
}
