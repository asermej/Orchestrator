using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for Interview entities
/// </summary>
internal sealed class InterviewDataManager
{
    private readonly string _dbConnectionString;

    public InterviewDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<Interview>();
    }

    public async Task<Interview?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, job_id, applicant_id, agent_id, interview_configuration_id, token, status, interview_type, scheduled_at, started_at, completed_at, current_question_index, created_at, updated_at, is_deleted
            FROM interviews
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Interview>(sql, new { id });
    }

    public async Task<Interview?> GetByToken(string token)
    {
        const string sql = @"
            SELECT id, job_id, applicant_id, agent_id, interview_configuration_id, token, status, interview_type, scheduled_at, started_at, completed_at, current_question_index, created_at, updated_at, is_deleted
            FROM interviews
            WHERE token = @Token AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<Interview>(sql, new { Token = token });
    }

    public async Task<Interview> Add(Interview interview)
    {
        if (interview.Id == Guid.Empty)
        {
            interview.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO interviews (id, job_id, applicant_id, agent_id, interview_configuration_id, token, status, interview_type, scheduled_at, created_by)
            VALUES (@Id, @JobId, @ApplicantId, @AgentId, @InterviewConfigurationId, @Token, @Status, @InterviewType, @ScheduledAt, @CreatedBy)
            RETURNING id, job_id, applicant_id, agent_id, interview_configuration_id, token, status, interview_type, scheduled_at, started_at, completed_at, current_question_index, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<Interview>(sql, interview);
        return newItem!;
    }

    public async Task<Interview> Update(Interview interview)
    {
        const string sql = @"
            UPDATE interviews
            SET
                status = @Status,
                started_at = @StartedAt,
                completed_at = @CompletedAt,
                current_question_index = @CurrentQuestionIndex,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, job_id, applicant_id, agent_id, interview_configuration_id, token, status, interview_type, scheduled_at, started_at, completed_at, current_question_index, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<Interview>(sql, interview);
        if (updatedItem == null)
        {
            throw new InterviewNotFoundException("Interview not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE interviews
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }

    public async Task<PaginatedResult<Interview>> Search(Guid? groupId, Guid? jobId, Guid? applicantId, Guid? agentId, string? status, int pageNumber, int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();
        var joinSql = "";

        if (groupId.HasValue)
        {
            joinSql = "INNER JOIN agents a ON i.agent_id = a.id";
            whereClauses.Add("a.group_id = @GroupId");
            parameters.Add("GroupId", groupId.Value);
        }

        if (jobId.HasValue)
        {
            whereClauses.Add("i.job_id = @JobId");
            parameters.Add("JobId", jobId.Value);
        }

        if (applicantId.HasValue)
        {
            whereClauses.Add("i.applicant_id = @ApplicantId");
            parameters.Add("ApplicantId", applicantId.Value);
        }

        if (agentId.HasValue)
        {
            whereClauses.Add("i.agent_id = @AgentId");
            parameters.Add("AgentId", agentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereClauses.Add("i.status = @Status");
            parameters.Add("Status", status);
        }

        whereClauses.Add("i.is_deleted = false");

        var whereSql = $"WHERE {string.Join(" AND ", whereClauses)}";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var countSql = $"SELECT COUNT(*) FROM interviews i {joinSql} {whereSql}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (pageNumber - 1) * pageSize;
        var querySql = $@"
            SELECT i.id, i.job_id, i.applicant_id, i.agent_id, i.interview_configuration_id, i.token, i.status, i.interview_type, i.scheduled_at, i.started_at, i.completed_at, i.current_question_index, i.created_at, i.updated_at, i.is_deleted
            FROM interviews i
            {joinSql}
            {whereSql}
            ORDER BY i.created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<Interview>(querySql, parameters);

        return new PaginatedResult<Interview>(items, totalCount, pageNumber, pageSize);
    }
}
