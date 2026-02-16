using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a FollowUpTemplate in the domain
/// </summary>
[Table("follow_up_templates")]
public class FollowUpTemplate : Entity
{
    [Column("interview_question_id")]
    public Guid? InterviewQuestionId { get; set; }

    [Column("competency_tag")]
    public string? CompetencyTag { get; set; }

    [Column("trigger_hints")]
    public string[]? TriggerHints { get; set; }

    [Column("canonical_text")]
    public string CanonicalText { get; set; } = string.Empty;

    [Column("allow_paraphrase")]
    public bool AllowParaphrase { get; set; } = false;

    [Column("is_approved")]
    public bool IsApproved { get; set; } = false;
}
