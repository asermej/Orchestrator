using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Per-competency response record for an AI-conducted interview: holistic competency score (1-5),
/// AI rationale, questions asked, and response text/audio.
/// </summary>
[Table("competency_responses")]
public class CompetencyResponse : Entity
{
    [Column("interview_id")]
    public Guid InterviewId { get; set; }

    [Column("competency_id")]
    public Guid CompetencyId { get; set; }

    [Column("competency_score")]
    public int CompetencyScore { get; set; }

    [Column("competency_rationale")]
    public string? CompetencyRationale { get; set; }

    [Column("follow_up_count")]
    public int FollowUpCount { get; set; }

    [Column("questions_asked")]
    public string? QuestionsAsked { get; set; }

    [Column("response_text")]
    public string? ResponseText { get; set; }

    [Column("response_audio_url")]
    public string? ResponseAudioUrl { get; set; }

    [Column("scoring_weight")]
    public int? ScoringWeight { get; set; }

    [Column("generated_question_text")]
    public string? GeneratedQuestionText { get; set; }

    [Column("competency_transcript")]
    public string? CompetencyTranscript { get; set; }

    [Column("competency_skipped")]
    public bool CompetencySkipped { get; set; }

    [Column("skip_reason")]
    public string? SkipReason { get; set; }
}
