using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for FollowUpSelectionLog entities
/// </summary>
internal sealed class FollowUpSelectionLogDataManager
{
    private readonly string _dbConnectionString;

    public FollowUpSelectionLogDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<FollowUpSelectionLog>();
    }

    public async Task<FollowUpSelectionLog?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, interview_id, interview_question_id, answer_excerpt, candidate_template_ids_presented, selected_template_id, matched_competency_tag, rationale, method, timestamp, created_at, updated_at, is_deleted
            FROM follow_up_selection_logs
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<FollowUpSelectionLog>(sql, new { id });
    }

    public async Task<IEnumerable<FollowUpSelectionLog>> GetByInterviewId(Guid interviewId)
    {
        const string sql = @"
            SELECT id, interview_id, interview_question_id, answer_excerpt, candidate_template_ids_presented, selected_template_id, matched_competency_tag, rationale, method, timestamp, created_at, updated_at, is_deleted
            FROM follow_up_selection_logs
            WHERE interview_id = @interviewId AND is_deleted = false
            ORDER BY timestamp DESC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<FollowUpSelectionLog>(sql, new { interviewId });
    }

    public async Task<FollowUpSelectionLog> Add(FollowUpSelectionLog log)
    {
        if (log.Id == Guid.Empty)
        {
            log.Id = Guid.NewGuid();
        }

        if (log.Timestamp == default)
        {
            log.Timestamp = DateTime.UtcNow;
        }

        const string sql = @"
            INSERT INTO follow_up_selection_logs (id, interview_id, interview_question_id, answer_excerpt, candidate_template_ids_presented, selected_template_id, matched_competency_tag, rationale, method, timestamp, created_at, updated_at, is_deleted)
            VALUES (@Id, @InterviewId, @InterviewQuestionId, @AnswerExcerpt, @CandidateTemplateIdsPresented, @SelectedTemplateId, @MatchedCompetencyTag, @Rationale, @Method, @Timestamp, @CreatedAt, @UpdatedAt, @IsDeleted)
            RETURNING id, interview_id, interview_question_id, answer_excerpt, candidate_template_ids_presented, selected_template_id, matched_competency_tag, rationale, method, timestamp, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<FollowUpSelectionLog>(sql, log);
        return newItem!;
    }
}
