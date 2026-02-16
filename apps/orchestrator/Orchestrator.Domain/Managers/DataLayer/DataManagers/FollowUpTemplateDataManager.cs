using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for FollowUpTemplate entities
/// </summary>
internal sealed class FollowUpTemplateDataManager
{
    private readonly string _dbConnectionString;

    public FollowUpTemplateDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<FollowUpTemplate>();
    }

    public async Task<FollowUpTemplate?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, interview_question_id, competency_tag, trigger_hints, canonical_text, allow_paraphrase, is_approved, created_by, created_at, updated_at, is_deleted
            FROM follow_up_templates
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<FollowUpTemplate>(sql, new { id });
    }

    public async Task<IEnumerable<FollowUpTemplate>> GetByInterviewQuestionId(Guid interviewQuestionId)
    {
        const string sql = @"
            SELECT id, interview_question_id, competency_tag, trigger_hints, canonical_text, allow_paraphrase, is_approved, created_by, created_at, updated_at, is_deleted
            FROM follow_up_templates
            WHERE interview_question_id = @interviewQuestionId AND is_deleted = false
            ORDER BY created_at ASC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<FollowUpTemplate>(sql, new { interviewQuestionId });
    }

    public async Task<IEnumerable<FollowUpTemplate>> GetApprovedByInterviewQuestionId(Guid interviewQuestionId)
    {
        const string sql = @"
            SELECT id, interview_question_id, competency_tag, trigger_hints, canonical_text, allow_paraphrase, is_approved, created_by, created_at, updated_at, is_deleted
            FROM follow_up_templates
            WHERE interview_question_id = @interviewQuestionId AND is_approved = true AND is_deleted = false
            ORDER BY created_at ASC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<FollowUpTemplate>(sql, new { interviewQuestionId });
    }

    public async Task<FollowUpTemplate> Add(FollowUpTemplate template)
    {
        if (template.Id == Guid.Empty)
        {
            template.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO follow_up_templates (id, interview_question_id, competency_tag, trigger_hints, canonical_text, allow_paraphrase, is_approved, created_by, created_at, updated_at, is_deleted)
            VALUES (@Id, @InterviewQuestionId, @CompetencyTag, @TriggerHints, @CanonicalText, @AllowParaphrase, @IsApproved, @CreatedBy, @CreatedAt, @UpdatedAt, @IsDeleted)
            RETURNING id, interview_question_id, competency_tag, trigger_hints, canonical_text, allow_paraphrase, is_approved, created_by, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<FollowUpTemplate>(sql, template);
        return newItem!;
    }

    public async Task<FollowUpTemplate> Update(FollowUpTemplate template)
    {
        const string sql = @"
            UPDATE follow_up_templates
            SET
                competency_tag = @CompetencyTag,
                trigger_hints = @TriggerHints,
                canonical_text = @CanonicalText,
                allow_paraphrase = @AllowParaphrase,
                is_approved = @IsApproved,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, interview_question_id, competency_tag, trigger_hints, canonical_text, allow_paraphrase, is_approved, created_by, created_at, updated_at, is_deleted";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<FollowUpTemplate>(sql, template);
        if (updatedItem == null)
        {
            throw new FollowUpTemplateNotFoundException("Follow-up template not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task BulkApprove(List<Guid> templateIds)
    {
        if (templateIds == null || !templateIds.Any())
        {
            return;
        }

        const string sql = @"
            UPDATE follow_up_templates
            SET is_approved = true, updated_at = CURRENT_TIMESTAMP
            WHERE id = ANY(@templateIds) AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        await connection.ExecuteAsync(sql, new { templateIds = templateIds.ToArray() });
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE follow_up_templates
            SET is_deleted = true, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }
}

/// <summary>
/// Exception thrown when a follow-up template is not found
/// </summary>
public class FollowUpTemplateNotFoundException : Exception
{
    public FollowUpTemplateNotFoundException(string message) : base(message) { }
}
