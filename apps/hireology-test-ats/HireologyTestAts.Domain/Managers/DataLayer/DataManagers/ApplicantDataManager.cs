using Dapper;
using Npgsql;

namespace HireologyTestAts.Domain;

internal sealed class ApplicantDataManager
{
    private readonly string _connectionString;

    public ApplicantDataManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<Applicant>> ListAsync(int pageNumber, int pageSize, IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        var offset = (pageNumber - 1) * pageSize;
        string sql;
        object? args;
        if (allowedOrganizationIds != null && allowedOrganizationIds.Count > 0)
        {
            sql = @"
            SELECT id AS Id, job_id AS JobId, organization_id AS OrganizationId,
                   first_name AS FirstName, last_name AS LastName, email AS Email, phone AS Phone,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM applicants
            WHERE organization_id = ANY(@OrganizationIds)
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";
            args = new { OrganizationIds = allowedOrganizationIds.ToArray(), PageSize = pageSize, Offset = offset };
        }
        else
        {
            sql = @"
            SELECT id AS Id, job_id AS JobId, organization_id AS OrganizationId,
                   first_name AS FirstName, last_name AS LastName, email AS Email, phone AS Phone,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM applicants
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";
            args = new { PageSize = pageSize, Offset = offset };
        }
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<Applicant>(new CommandDefinition(sql, args));
        return items.ToList();
    }

    public async Task<int> CountAsync(IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        string sql;
        object? args;
        if (allowedOrganizationIds != null && allowedOrganizationIds.Count > 0)
        {
            sql = "SELECT COUNT(*) FROM applicants WHERE organization_id = ANY(@OrganizationIds)";
            args = new { OrganizationIds = allowedOrganizationIds.ToArray() };
        }
        else
        {
            sql = "SELECT COUNT(*) FROM applicants";
            args = null;
        }
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, args ?? new { }));
    }

    public async Task<IReadOnlyList<Applicant>> ListByJobIdAsync(Guid jobId)
    {
        const string sql = @"
            SELECT id AS Id, job_id AS JobId, organization_id AS OrganizationId,
                   first_name AS FirstName, last_name AS LastName, email AS Email, phone AS Phone,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM applicants
            WHERE job_id = @JobId
            ORDER BY created_at DESC";
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<Applicant>(new CommandDefinition(sql, new { JobId = jobId }));
        return items.ToList();
    }

    public async Task<Applicant?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id AS Id, job_id AS JobId, organization_id AS OrganizationId,
                   first_name AS FirstName, last_name AS LastName, email AS Email, phone AS Phone,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM applicants WHERE id = @Id";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Applicant>(new CommandDefinition(sql, new { Id = id }));
    }

    public async Task<Applicant> CreateAsync(Applicant applicant)
    {
        if (applicant.Id == Guid.Empty) applicant.Id = Guid.NewGuid();
        applicant.CreatedAt = DateTime.UtcNow;
        applicant.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO applicants (id, job_id, organization_id, first_name, last_name, email, phone, created_at, updated_at)
            VALUES (@Id, @JobId, @OrganizationId, @FirstName, @LastName, @Email, @Phone, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, job_id AS JobId, organization_id AS OrganizationId,
                      first_name AS FirstName, last_name AS LastName, email AS Email, phone AS Phone,
                      created_at AS CreatedAt, updated_at AS UpdatedAt";
        await using var conn = new NpgsqlConnection(_connectionString);
        return (await conn.QuerySingleAsync<Applicant>(new CommandDefinition(sql, applicant)))!;
    }
}
