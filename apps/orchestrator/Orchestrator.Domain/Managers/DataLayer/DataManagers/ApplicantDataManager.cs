using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for Applicant entities
/// </summary>
internal sealed class ApplicantDataManager
{
    private readonly string _dbConnectionString;

    public ApplicantDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<Applicant>();
    }

    public async Task<Applicant?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, organization_id, external_applicant_id, first_name, last_name, email, phone, created_at, updated_at, is_deleted
            FROM applicants
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Applicant>(sql, new { id });
    }

    public async Task<Applicant?> GetByExternalId(Guid organizationId, string externalApplicantId)
    {
        const string sql = @"
            SELECT id, organization_id, external_applicant_id, first_name, last_name, email, phone, created_at, updated_at, is_deleted
            FROM applicants
            WHERE organization_id = @OrganizationId AND external_applicant_id = @ExternalApplicantId AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Applicant>(sql, new { OrganizationId = organizationId, ExternalApplicantId = externalApplicantId });
    }

    public async Task<Applicant> Add(Applicant applicant)
    {
        if (applicant.Id == Guid.Empty)
        {
            applicant.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO applicants (id, organization_id, external_applicant_id, first_name, last_name, email, phone, created_by)
            VALUES (@Id, @OrganizationId, @ExternalApplicantId, @FirstName, @LastName, @Email, @Phone, @CreatedBy)
            RETURNING id, organization_id, external_applicant_id, first_name, last_name, email, phone, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<Applicant>(sql, applicant);
        return newItem!;
    }

    public async Task<Applicant> Update(Applicant applicant)
    {
        const string sql = @"
            UPDATE applicants
            SET
                first_name = @FirstName,
                last_name = @LastName,
                email = @Email,
                phone = @Phone,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, organization_id, external_applicant_id, first_name, last_name, email, phone, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<Applicant>(sql, applicant);
        if (updatedItem == null)
        {
            throw new ApplicantNotFoundException("Applicant not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE applicants
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<Applicant>> Search(Guid? organizationId, string? email, string? name, int pageNumber, int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (organizationId.HasValue)
        {
            whereClauses.Add("organization_id = @OrganizationId");
            parameters.Add("OrganizationId", organizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            whereClauses.Add("email ILIKE @Email");
            parameters.Add("Email", $"%{email}%");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            whereClauses.Add("(first_name ILIKE @Name OR last_name ILIKE @Name)");
            parameters.Add("Name", $"%{name}%");
        }

        whereClauses.Add("is_deleted = false");

        var whereSql = $"WHERE {string.Join(" AND ", whereClauses)}";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var countSql = $"SELECT COUNT(*) FROM applicants {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT id, organization_id, external_applicant_id, first_name, last_name, email, phone, created_at, updated_at, is_deleted
            FROM applicants
            {whereSql}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<Applicant>(querySql, parameters);

        return new PaginatedResult<Applicant>(items, totalCount, pageNumber, pageSize);
    }
}
