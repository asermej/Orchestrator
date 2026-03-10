using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

[Table("role_templates")]
public class RoleTemplate : Entity
{
    [Column("role_key")]
    public string RoleKey { get; set; } = string.Empty;

    [Column("role_name")]
    public string RoleName { get; set; } = string.Empty;

    [Column("industry")]
    public string Industry { get; set; } = string.Empty;

    [Column("source")]
    public string Source { get; set; } = "system";

    [Column("group_id")]
    public Guid? GroupId { get; set; }

    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    [Column("visibility_scope")]
    public string VisibilityScope { get; set; } = Domain.VisibilityScope.OrganizationOnly;

    [Column("max_follow_ups_per_question")]
    public int MaxFollowUpsPerQuestion { get; set; } = 2;

    [Column("scoring_scale_min")]
    public int ScoringScaleMin { get; set; } = 1;

    [Column("scoring_scale_max")]
    public int ScoringScaleMax { get; set; } = 5;

    [Column("flag_threshold")]
    public int FlagThreshold { get; set; } = 2;

    [NotMapped]
    public List<Competency> Competencies { get; set; } = new();

    /// <summary>
    /// Set by list queries via subquery; when Competencies is not loaded, use this for count.
    /// </summary>
    [NotMapped]
    public int CompetencyCount { get; set; }
}
