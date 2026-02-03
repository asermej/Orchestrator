using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for InterviewResponse entities
/// </summary>
internal sealed class InterviewResponseDataManager
{
    private readonly string _dbConnectionString;

    public InterviewResponseDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<InterviewResponse>();
    }

    public async Task<InterviewResponse?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, interview_id, question_id, question_text, transcript, audio_url, duration_seconds, response_order, is_follow_up, ai_analysis, created_at, updated_at, is_deleted
            FROM interview_responses
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewResponse>(sql, new { id });
    }

    public async Task<IEnumerable<InterviewResponse>> GetByInterviewId(Guid interviewId)
    {
        const string sql = @"
            SELECT id, interview_id, question_id, question_text, transcript, audio_url, duration_seconds, response_order, is_follow_up, ai_analysis, created_at, updated_at, is_deleted
            FROM interview_responses
            WHERE interview_id = @InterviewId AND is_deleted = false
            ORDER BY response_order ASC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<InterviewResponse>(sql, new { InterviewId = interviewId });
    }

    public async Task<InterviewResponse> Add(InterviewResponse response)
    {
        if (response.Id == Guid.Empty)
        {
            response.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO interview_responses (id, interview_id, question_id, question_text, transcript, audio_url, duration_seconds, response_order, is_follow_up, ai_analysis, created_by)
            VALUES (@Id, @InterviewId, @QuestionId, @QuestionText, @Transcript, @AudioUrl, @DurationSeconds, @ResponseOrder, @IsFollowUp, @AiAnalysis, @CreatedBy)
            RETURNING id, interview_id, question_id, question_text, transcript, audio_url, duration_seconds, response_order, is_follow_up, ai_analysis, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<InterviewResponse>(sql, response);
        return newItem!;
    }

    public async Task<InterviewResponse> Update(InterviewResponse response)
    {
        const string sql = @"
            UPDATE interview_responses
            SET
                transcript = @Transcript,
                audio_url = @AudioUrl,
                duration_seconds = @DurationSeconds,
                ai_analysis = @AiAnalysis,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, interview_id, question_id, question_text, transcript, audio_url, duration_seconds, response_order, is_follow_up, ai_analysis, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<InterviewResponse>(sql, response);
        if (updatedItem == null)
        {
            throw new InterviewNotFoundException("Interview response not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE interview_responses
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }
}
