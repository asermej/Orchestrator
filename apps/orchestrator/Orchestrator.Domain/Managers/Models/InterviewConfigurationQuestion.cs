using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a question within an Interview Configuration
/// </summary>
[Table("interview_configuration_questions")]
public class InterviewConfigurationQuestion
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("interview_configuration_id")]
    public Guid InterviewConfigurationId { get; set; }

    [Column("question")]
    public string Question { get; set; } = string.Empty;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("scoring_weight")]
    public decimal ScoringWeight { get; set; } = 1.0m;

    [Column("scoring_guidance")]
    public string? ScoringGuidance { get; set; }

    [Column("follow_ups_enabled")]
    public bool FollowUpsEnabled { get; set; } = true;

    [Column("max_follow_ups")]
    public int MaxFollowUps { get; set; } = 2;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
