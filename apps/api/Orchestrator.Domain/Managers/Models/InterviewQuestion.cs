using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents an InterviewQuestion in the domain
/// </summary>
[Table("interview_questions")]
public class InterviewQuestion : Entity
{
    [Column("question_text")]
    public string QuestionText { get; set; } = string.Empty;

    [Column("question_order")]
    public int QuestionOrder { get; set; }

    [Column("is_required")]
    public bool IsRequired { get; set; } = true;

    [Column("follow_up_prompt")]
    public string? FollowUpPrompt { get; set; }

    [Column("max_follow_ups")]
    public int MaxFollowUps { get; set; } = 2;

    [Column("follow_ups_enabled")]
    public bool FollowUpsEnabled { get; set; } = true;
}
