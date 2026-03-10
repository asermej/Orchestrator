using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

internal sealed class CompetencyResponseDataManager
{
    private readonly string _dbConnectionString;

    public CompetencyResponseDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<CompetencyResponse>();
    }

    public async Task<List<CompetencyResponse>> GetByInterviewId(Guid interviewId)
    {
        const string sql = @"
            SELECT id, interview_id, competency_id, competency_score, competency_rationale,
                   follow_up_count, scoring_weight, competency_transcript,
                   questions_asked, generated_question_text, response_text, response_audio_url,
                   competency_skipped, skip_reason,
                   created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by
            FROM competency_responses
            WHERE interview_id = @InterviewId AND is_deleted = false
            ORDER BY competency_id";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var results = await connection.QueryAsync<CompetencyResponse>(sql, new { InterviewId = interviewId });
        return results.ToList();
    }

    public async Task<CompetencyResponse> Upsert(CompetencyResponse response)
    {
        if (response.Id == Guid.Empty)
            response.Id = Guid.NewGuid();

        const string sql = @"
            INSERT INTO competency_responses (id, interview_id, competency_id, competency_score, competency_rationale,
                follow_up_count, scoring_weight, competency_transcript,
                questions_asked, generated_question_text, response_text, response_audio_url,
                competency_skipped, skip_reason, created_by)
            VALUES (@Id, @InterviewId, @CompetencyId, @CompetencyScore, @CompetencyRationale,
                @FollowUpCount, @ScoringWeight, @CompetencyTranscript,
                @QuestionsAsked::jsonb, @GeneratedQuestionText, @ResponseText, @ResponseAudioUrl,
                @CompetencySkipped, @SkipReason, @CreatedBy)
            ON CONFLICT (interview_id, competency_id)
            DO UPDATE SET
                competency_score = EXCLUDED.competency_score,
                competency_rationale = EXCLUDED.competency_rationale,
                follow_up_count = EXCLUDED.follow_up_count,
                scoring_weight = EXCLUDED.scoring_weight,
                competency_transcript = EXCLUDED.competency_transcript,
                questions_asked = EXCLUDED.questions_asked,
                generated_question_text = EXCLUDED.generated_question_text,
                response_text = EXCLUDED.response_text,
                response_audio_url = EXCLUDED.response_audio_url,
                competency_skipped = EXCLUDED.competency_skipped,
                skip_reason = EXCLUDED.skip_reason,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = EXCLUDED.created_by
            RETURNING id, interview_id, competency_id, competency_score, competency_rationale,
                      follow_up_count, scoring_weight, competency_transcript,
                      questions_asked, generated_question_text, response_text, response_audio_url,
                      competency_skipped, skip_reason,
                      created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var result = await connection.QueryFirstOrDefaultAsync<CompetencyResponse>(sql, response);
        return result!;
    }
}
