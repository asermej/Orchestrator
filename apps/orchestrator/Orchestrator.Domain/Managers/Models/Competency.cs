using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

[Table("competencies")]
public class Competency : Entity
{
    [Column("role_template_id")]
    public Guid RoleTemplateId { get; set; }

    [Column("competency_key")]
    public string CompetencyKey { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("default_weight")]
    public int DefaultWeight { get; set; }

    [Column("is_required")]
    public bool IsRequired { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("canonical_example")]
    public string? CanonicalExample { get; set; }
}
