using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a FollowUpSelectionLog in the domain
/// </summary>
[Table("follow_up_selection_logs")]
public class FollowUpSelectionLog : Entity
{
    [Column("interview_id")]
    public Guid InterviewId { get; set; }

    [Column("interview_question_id")]
    public Guid InterviewQuestionId { get; set; }

    [Column("answer_excerpt")]
    public string? AnswerExcerpt { get; set; }

    [Column("candidate_template_ids_presented")]
    public Guid[]? CandidateTemplateIdsPresented { get; set; }

    [Column("selected_template_id")]
    public Guid? SelectedTemplateId { get; set; }

    [Column("matched_competency_tag")]
    public string? MatchedCompetencyTag { get; set; }

    [Column("rationale")]
    public string? Rationale { get; set; }

    [Column("method")]
    public string Method { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; }
}
