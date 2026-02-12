using Dapper;
using Npgsql;
using HireologyTestAts.Api.Models;

namespace HireologyTestAts.Api.Services;

public class JobsRepository
{
    private readonly string _connectionString;

    public JobsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HireologyTestAts")
            ?? throw new InvalidOperationException("ConnectionStrings:HireologyTestAts is required");
    }

    public async Task<IReadOnlyList<JobItem>> ListAsync(int pageNumber, int pageSize, IReadOnlyList<Guid>? allowedOrganizationIds = null, CancellationToken ct = default)
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
        var items = await conn.QueryAsync<JobItem>(new CommandDefinition(sql, args, cancellationToken: ct));
        return items.ToList();
    }

    public async Task<int> CountAsync(IReadOnlyList<Guid>? allowedOrganizationIds = null, CancellationToken ct = default)
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
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, args ?? new { }, cancellationToken: ct));
    }

    public async Task<JobItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, external_job_id AS ExternalJobId, title AS Title, description AS Description,
                   location AS Location, status AS Status, organization_id AS OrganizationId, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM jobs WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<JobItem>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<JobItem?> GetByExternalIdAsync(string externalJobId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, external_job_id AS ExternalJobId, title AS Title, description AS Description,
                   location AS Location, status AS Status, organization_id AS OrganizationId, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM jobs WHERE external_job_id = @ExternalJobId";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<JobItem>(new CommandDefinition(sql, new { ExternalJobId = externalJobId }, cancellationToken: ct));
    }

    public async Task<JobItem> CreateAsync(JobItem job, CancellationToken ct = default)
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
        return (await conn.QuerySingleAsync<JobItem>(new CommandDefinition(sql, job, cancellationToken: ct)))!;
    }

    public async Task<JobItem?> UpdateAsync(JobItem job, CancellationToken ct = default)
    {
        job.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE jobs
            SET title = @Title, description = @Description, location = @Location, status = @Status, organization_id = @OrganizationId, updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING id AS Id, external_job_id AS ExternalJobId, title AS Title, description AS Description,
                      location AS Location, status AS Status, organization_id AS OrganizationId, created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<JobItem>(new CommandDefinition(sql, job, cancellationToken: ct));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM jobs WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return rows > 0;
    }
}
