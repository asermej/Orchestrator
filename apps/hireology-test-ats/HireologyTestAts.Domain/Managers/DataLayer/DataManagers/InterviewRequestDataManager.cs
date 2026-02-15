using Dapper;
using Npgsql;

namespace HireologyTestAts.Domain;

internal sealed class InterviewRequestDataManager
{
    private readonly string _connectionString;

    public InterviewRequestDataManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<InterviewRequest> CreateAsync(InterviewRequest request)
    {
        if (request.Id == Guid.Empty) request.Id = Guid.NewGuid();
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO interview_requests 
                (id, applicant_id, job_id, orchestrator_interview_id, invite_url, short_code, 
                 status, score, result_summary, result_recommendation, result_strengths, 
                 result_areas_for_improvement, webhook_received_at, created_at, updated_at)
            VALUES 
                (@Id, @ApplicantId, @JobId, @OrchestratorInterviewId, @InviteUrl, @ShortCode,
                 @Status, @Score, @ResultSummary, @ResultRecommendation, @ResultStrengths,
                 @ResultAreasForImprovement, @WebhookReceivedAt, @CreatedAt, @UpdatedAt)
            RETURNING 
                id AS Id, applicant_id AS ApplicantId, job_id AS JobId,
                orchestrator_interview_id AS OrchestratorInterviewId, invite_url AS InviteUrl,
                short_code AS ShortCode, status AS Status, score AS Score,
                result_summary AS ResultSummary, result_recommendation AS ResultRecommendation,
                result_strengths AS ResultStrengths, result_areas_for_improvement AS ResultAreasForImprovement,
                webhook_received_at AS WebhookReceivedAt, created_at AS CreatedAt, updated_at AS UpdatedAt";

        await using var conn = new NpgsqlConnection(_connectionString);
        return (await conn.QuerySingleAsync<InterviewRequest>(new CommandDefinition(sql, request)))!;
    }

    public async Task<InterviewRequest?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id AS Id, applicant_id AS ApplicantId, job_id AS JobId,
                   orchestrator_interview_id AS OrchestratorInterviewId, invite_url AS InviteUrl,
                   short_code AS ShortCode, status AS Status, score AS Score,
                   result_summary AS ResultSummary, result_recommendation AS ResultRecommendation,
                   result_strengths AS ResultStrengths, result_areas_for_improvement AS ResultAreasForImprovement,
                   webhook_received_at AS WebhookReceivedAt, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM interview_requests WHERE id = @Id";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<InterviewRequest>(new CommandDefinition(sql, new { Id = id }));
    }

    public async Task<InterviewRequest?> GetByApplicantIdAsync(Guid applicantId)
    {
        const string sql = @"
            SELECT id AS Id, applicant_id AS ApplicantId, job_id AS JobId,
                   orchestrator_interview_id AS OrchestratorInterviewId, invite_url AS InviteUrl,
                   short_code AS ShortCode, status AS Status, score AS Score,
                   result_summary AS ResultSummary, result_recommendation AS ResultRecommendation,
                   result_strengths AS ResultStrengths, result_areas_for_improvement AS ResultAreasForImprovement,
                   webhook_received_at AS WebhookReceivedAt, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM interview_requests WHERE applicant_id = @ApplicantId
            ORDER BY created_at DESC LIMIT 1";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<InterviewRequest>(new CommandDefinition(sql, new { ApplicantId = applicantId }));
    }

    public async Task<InterviewRequest?> GetByOrchestratorInterviewIdAsync(Guid orchestratorInterviewId)
    {
        const string sql = @"
            SELECT id AS Id, applicant_id AS ApplicantId, job_id AS JobId,
                   orchestrator_interview_id AS OrchestratorInterviewId, invite_url AS InviteUrl,
                   short_code AS ShortCode, status AS Status, score AS Score,
                   result_summary AS ResultSummary, result_recommendation AS ResultRecommendation,
                   result_strengths AS ResultStrengths, result_areas_for_improvement AS ResultAreasForImprovement,
                   webhook_received_at AS WebhookReceivedAt, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM interview_requests WHERE orchestrator_interview_id = @OrchestratorInterviewId";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<InterviewRequest>(new CommandDefinition(sql, new { OrchestratorInterviewId = orchestratorInterviewId }));
    }

    public async Task<IReadOnlyList<InterviewRequest>> ListByJobIdAsync(Guid jobId)
    {
        const string sql = @"
            SELECT id AS Id, applicant_id AS ApplicantId, job_id AS JobId,
                   orchestrator_interview_id AS OrchestratorInterviewId, invite_url AS InviteUrl,
                   short_code AS ShortCode, status AS Status, score AS Score,
                   result_summary AS ResultSummary, result_recommendation AS ResultRecommendation,
                   result_strengths AS ResultStrengths, result_areas_for_improvement AS ResultAreasForImprovement,
                   webhook_received_at AS WebhookReceivedAt, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM interview_requests WHERE job_id = @JobId
            ORDER BY created_at DESC";

        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<InterviewRequest>(new CommandDefinition(sql, new { JobId = jobId }));
        return items.ToList();
    }

    public async Task<InterviewRequest> UpdateAsync(InterviewRequest request)
    {
        request.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            UPDATE interview_requests SET
                status = @Status,
                invite_url = @InviteUrl,
                short_code = @ShortCode,
                score = @Score,
                result_summary = @ResultSummary,
                result_recommendation = @ResultRecommendation,
                result_strengths = @ResultStrengths,
                result_areas_for_improvement = @ResultAreasForImprovement,
                webhook_received_at = @WebhookReceivedAt,
                updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING 
                id AS Id, applicant_id AS ApplicantId, job_id AS JobId,
                orchestrator_interview_id AS OrchestratorInterviewId, invite_url AS InviteUrl,
                short_code AS ShortCode, status AS Status, score AS Score,
                result_summary AS ResultSummary, result_recommendation AS ResultRecommendation,
                result_strengths AS ResultStrengths, result_areas_for_improvement AS ResultAreasForImprovement,
                webhook_received_at AS WebhookReceivedAt, created_at AS CreatedAt, updated_at AS UpdatedAt";

        await using var conn = new NpgsqlConnection(_connectionString);
        return (await conn.QuerySingleAsync<InterviewRequest>(new CommandDefinition(sql, request)))!;
    }
}
