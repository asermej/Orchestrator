using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a JobType (interview template) in the domain
/// </summary>
[Table("job_types")]
public class JobType : Entity
{
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
