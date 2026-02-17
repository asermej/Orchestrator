using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents an Interview session in the domain
/// </summary>
[Table("interviews")]
public class Interview : Entity
{
    [Column("job_id")]
    public Guid JobId { get; set; }

    [Column("applicant_id")]
    public Guid ApplicantId { get; set; }

    [Column("agent_id")]
    public Guid AgentId { get; set; }

    [Column("interview_configuration_id")]
    public Guid? InterviewConfigurationId { get; set; }

    [Column("interview_guide_id")]
    public Guid? InterviewGuideId { get; set; }

    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("interview_type")]
    public string InterviewType { get; set; } = "voice";

    [Column("scheduled_at")]
    public DateTime? ScheduledAt { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("current_question_index")]
    public int CurrentQuestionIndex { get; set; } = 0;
}

/// <summary>
/// Interview status constants
/// </summary>
public static class InterviewStatus
{
    public const string Pending = "pending";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string Expired = "expired";
}

/// <summary>
/// Interview type constants
/// </summary>
public static class InterviewType
{
    public const string Voice = "voice";
    public const string Text = "text";
}
