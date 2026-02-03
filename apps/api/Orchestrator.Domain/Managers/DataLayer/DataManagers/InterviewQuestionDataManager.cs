using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for InterviewQuestion entities
/// </summary>
internal sealed class InterviewQuestionDataManager
{
    private readonly string _dbConnectionString;

    public InterviewQuestionDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<InterviewQuestion>();
    }

    public async Task<InterviewQuestion?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, job_type_id, question_text, question_order, is_required, follow_up_prompt, max_follow_ups, created_at, updated_at, is_deleted
            FROM interview_questions
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewQuestion>(sql, new { id });
    }

    public async Task<IEnumerable<InterviewQuestion>> GetByJobTypeId(Guid jobTypeId)
    {
        const string sql = @"
            SELECT id, job_type_id, question_text, question_order, is_required, follow_up_prompt, max_follow_ups, created_at, updated_at, is_deleted
            FROM interview_questions
            WHERE job_type_id = @JobTypeId AND is_deleted = false
            ORDER BY question_order ASC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<InterviewQuestion>(sql, new { JobTypeId = jobTypeId });
    }

    public async Task<InterviewQuestion> Add(InterviewQuestion question)
    {
        if (question.Id == Guid.Empty)
        {
            question.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO interview_questions (id, job_type_id, question_text, question_order, is_required, follow_up_prompt, max_follow_ups, created_by)
            VALUES (@Id, @JobTypeId, @QuestionText, @QuestionOrder, @IsRequired, @FollowUpPrompt, @MaxFollowUps, @CreatedBy)
            RETURNING id, job_type_id, question_text, question_order, is_required, follow_up_prompt, max_follow_ups, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<InterviewQuestion>(sql, question);
        return newItem!;
    }

    public async Task<InterviewQuestion> Update(InterviewQuestion question)
    {
        const string sql = @"
            UPDATE interview_questions
            SET
                question_text = @QuestionText,
                question_order = @QuestionOrder,
                is_required = @IsRequired,
                follow_up_prompt = @FollowUpPrompt,
                max_follow_ups = @MaxFollowUps,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, job_type_id, question_text, question_order, is_required, follow_up_prompt, max_follow_ups, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<InterviewQuestion>(sql, question);
        if (updatedItem == null)
        {
            throw new JobTypeNotFoundException("Interview question not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE interview_questions
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }
}
