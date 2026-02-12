using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents an InterviewResponse in the domain
/// </summary>
[Table("interview_responses")]
public class InterviewResponse : Entity
{
    [Column("interview_id")]
    public Guid InterviewId { get; set; }

    [Column("question_id")]
    public Guid? QuestionId { get; set; }

    [Column("question_text")]
    public string QuestionText { get; set; } = string.Empty;

    [Column("transcript")]
    public string? Transcript { get; set; }

    [Column("audio_url")]
    public string? AudioUrl { get; set; }

    [Column("duration_seconds")]
    public int? DurationSeconds { get; set; }

    [Column("response_order")]
    public int ResponseOrder { get; set; }

    [Column("is_follow_up")]
    public bool IsFollowUp { get; set; } = false;

    [Column("follow_up_template_id")]
    public Guid? FollowUpTemplateId { get; set; }

    [Column("question_type")]
    public string QuestionType { get; set; } = "main";

    [Column("ai_analysis")]
    public string? AiAnalysis { get; set; }
}
