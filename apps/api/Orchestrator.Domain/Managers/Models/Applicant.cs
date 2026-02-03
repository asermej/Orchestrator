using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents an Applicant in the domain
/// </summary>
[Table("applicants")]
public class Applicant : Entity
{
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("external_applicant_id")]
    public string ExternalApplicantId { get; set; } = string.Empty;

    [Column("first_name")]
    public string? FirstName { get; set; }

    [Column("last_name")]
    public string? LastName { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }
}
