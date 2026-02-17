using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a reusable Interview Guide containing questions, scoring rubric,
/// and opening/closing templates. Used by InterviewConfigurations paired with an Agent.
/// </summary>
[Table("interview_guides")]
public class InterviewGuide : Entity
{
    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("opening_template")]
    public string? OpeningTemplate { get; set; }

    [Column("closing_template")]
    public string? ClosingTemplate { get; set; }

    [Column("scoring_rubric")]
    public string? ScoringRubric { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("visibility_scope")]
    public string VisibilityScope { get; set; } = Domain.VisibilityScope.OrganizationOnly;

    // Navigation property (not mapped to DB, populated by manager)
    [NotMapped]
    public List<InterviewGuideQuestion> Questions { get; set; } = new();

    // Computed property for question count (populated by search query)
    [NotMapped]
    public int QuestionCount { get; set; }
}
