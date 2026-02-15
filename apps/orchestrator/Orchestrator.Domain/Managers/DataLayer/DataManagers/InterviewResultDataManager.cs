using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for InterviewResult entities
/// </summary>
internal sealed class InterviewResultDataManager
{
    private readonly string _dbConnectionString;

    public InterviewResultDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<InterviewResult>();
    }

    public async Task<InterviewResult?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, interview_id, summary, score, recommendation, strengths, areas_for_improvement, full_transcript_url, webhook_sent_at, webhook_response, question_scores, created_at, updated_at, is_deleted
            FROM interview_results
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewResult>(sql, new { id });
    }

    public async Task<InterviewResult?> GetByInterviewId(Guid interviewId)
    {
        const string sql = @"
            SELECT id, interview_id, summary, score, recommendation, strengths, areas_for_improvement, full_transcript_url, webhook_sent_at, webhook_response, question_scores, created_at, updated_at, is_deleted
            FROM interview_results
            WHERE interview_id = @InterviewId AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewResult>(sql, new { InterviewId = interviewId });
    }

    public async Task<InterviewResult> Add(InterviewResult result)
    {
        if (result.Id == Guid.Empty)
        {
            result.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO interview_results (id, interview_id, summary, score, recommendation, strengths, areas_for_improvement, full_transcript_url, question_scores, created_by)
            VALUES (@Id, @InterviewId, @Summary, @Score, @Recommendation, @Strengths, @AreasForImprovement, @FullTranscriptUrl, @QuestionScores, @CreatedBy)
            RETURNING id, interview_id, summary, score, recommendation, strengths, areas_for_improvement, full_transcript_url, webhook_sent_at, webhook_response, question_scores, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<InterviewResult>(sql, result);
        return newItem!;
    }

    public async Task<InterviewResult> Update(InterviewResult result)
    {
        const string sql = @"
            UPDATE interview_results
            SET
                summary = @Summary,
                score = @Score,
                recommendation = @Recommendation,
                strengths = @Strengths,
                areas_for_improvement = @AreasForImprovement,
                full_transcript_url = @FullTranscriptUrl,
                webhook_sent_at = @WebhookSentAt,
                webhook_response = @WebhookResponse,
                question_scores = @QuestionScores,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, interview_id, summary, score, recommendation, strengths, areas_for_improvement, full_transcript_url, webhook_sent_at, webhook_response, question_scores, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<InterviewResult>(sql, result);
        if (updatedItem == null)
        {
            throw new InterviewNotFoundException("Interview result not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE interview_results
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }
}
