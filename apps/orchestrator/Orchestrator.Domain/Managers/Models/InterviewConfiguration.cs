using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents an Interview Configuration that defines how interviews are conducted
/// Includes the agent, questions, and scoring rubric
/// </summary>
[Table("interview_configurations")]
public class InterviewConfiguration : Entity
{
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("agent_id")]
    public Guid AgentId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("scoring_rubric")]
    public string? ScoringRubric { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation property (not mapped to DB, populated by manager)
    [NotMapped]
    public List<InterviewConfigurationQuestion> Questions { get; set; } = new();

    // Navigation property for the agent (not mapped to DB, populated by manager)
    [NotMapped]
    public Agent? Agent { get; set; }

    // Computed property for question count (populated by search query)
    [NotMapped]
    public int QuestionCount { get; set; }
}
