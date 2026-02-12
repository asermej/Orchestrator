using Dapper;
using Npgsql;

namespace HireologyTestAts.Domain;

internal sealed class JobDataManager
{
    private readonly string _connectionString;

    public JobDataManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<Job>> ListAsync(int pageNumber, int pageSize, IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        var offset = (pageNumber - 1) * pageSize;
        string sql;
        object? args;
        if (allowedOrganizationIds != null && allowedOrganizationIds.Count > 0)
        {
            sql = @"
            SELECT id AS Id, external_job_id AS ExternalJobId, title AS Title, description AS Description,
                   location AS Location, status AS Status, organization_id AS OrganizationId, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM jobs
            WHERE organization_id = ANY(@OrganizationIds)
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";
            args = new { OrganizationIds = allowedOrganizationIds.ToArray(), PageSize = pageSize, Offset = offset };
        }
        else
        {
            sql = @"
            SELECT id AS Id, external_job_id AS ExternalJobId, title AS Title, description AS Description,
                   location AS Location, status AS Status, organization_id AS OrganizationId, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM jobs
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";
            args = new { PageSize = pageSize, Offset = offset };
        }
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<Job>(new CommandDefinition(sql, args));
        return items.ToList();
    }

    public async Task<int> CountAsync(IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        string sql;
        object? args;
        if (allowedOrganizationIds != null && allowedOrganizationIds.Count > 0)
        {
            sql = "SELECT COUNT(*) FROM jobs WHERE organization_id = ANY(@OrganizationIds)";
            args = new { OrganizationIds = allowedOrganizationIds.ToArray() };
        }
        else
        {
            sql = "SELECT COUNT(*) FROM jobs";
            args = null;
        }
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, args ?? new { }));
    }

    public async Task<Job?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id AS Id, external_job_id AS ExternalJobId, title AS Title, description AS Description,
                   location AS Location, status AS Status, organization_id AS OrganizationId, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM jobs WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Job>(new CommandDefinition(sql, new { Id = id }));
    }

    public async Task<Job?> GetByExternalIdAsync(string externalJobId)
    {
        const string sql = @"
            SELECT id AS Id, external_job_id AS ExternalJobId, title AS Title, description AS Description,
                   location AS Location, status AS Status, organization_id AS OrganizationId, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM jobs WHERE external_job_id = @ExternalJobId";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Job>(new CommandDefinition(sql, new { ExternalJobId = externalJobId }));
    }

    public async Task<Job> CreateAsync(Job job)
    {
        if (job.Id == Guid.Empty) job.Id = Guid.NewGuid();
        job.CreatedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO jobs (id, external_job_id, title, description, location, status, organization_id, created_at, updated_at)
            VALUES (@Id, @ExternalJobId, @Title, @Description, @Location, @Status, @OrganizationId, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, external_job_id AS ExternalJobId, title AS Title, description AS Description,
                      location AS Location, status AS Status, organization_id AS OrganizationId, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return (await conn.QuerySingleAsync<Job>(new CommandDefinition(sql, job)))!;
    }

    public async Task<Job?> UpdateAsync(Job job)
    {
        job.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE jobs
            SET title = @Title, description = @Description, location = @Location, status = @Status, organization_id = @OrganizationId, updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING id AS Id, external_job_id AS ExternalJobId, title AS Title, description AS Description,
                      location AS Location, status AS Status, organization_id AS OrganizationId, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Job>(new CommandDefinition(sql, job));
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM jobs WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }));
        return rows > 0;
    }
}
