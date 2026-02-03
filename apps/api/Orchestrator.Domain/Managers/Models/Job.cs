using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a Job in the domain
/// </summary>
[Table("jobs")]
public class Job : Entity
{
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("job_type_id")]
    public Guid? JobTypeId { get; set; }

    [Column("external_job_id")]
    public string ExternalJobId { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("location")]
    public string? Location { get; set; }
}
