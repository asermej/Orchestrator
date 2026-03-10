using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Legacy Interview Configuration entity (replaced by InterviewTemplate).
/// </summary>
[Table("interview_configurations")]
public class InterviewConfiguration : Entity
{
    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    [Column("interview_guide_id")]
    public Guid InterviewGuideId { get; set; }

    [Column("agent_id")]
    public Guid AgentId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [NotMapped]
    public Agent? Agent { get; set; }

    // Computed property for question count from the linked guide (populated by search query)
    [NotMapped]
    public int QuestionCount { get; set; }
}
