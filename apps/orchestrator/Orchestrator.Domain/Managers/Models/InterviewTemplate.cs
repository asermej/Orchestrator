using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

[Table("interview_templates")]
public class InterviewTemplate : Entity
{
    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("role_template_id")]
    public Guid? RoleTemplateId { get; set; }

    [Column("agent_id")]
    public Guid? AgentId { get; set; }

    [Column("opening_template")]
    public string? OpeningTemplate { get; set; }

    [Column("closing_template")]
    public string? ClosingTemplate { get; set; }

    [NotMapped]
    public RoleTemplate? RoleTemplate { get; set; }

    [NotMapped]
    public Agent? Agent { get; set; }

    public const string DefaultOpeningTemplate = "Hi {{applicantName}}, my name is {{agentName}} and I'll be conducting your interview today for the {{jobTitle}} position.";
    public const string DefaultClosingTemplate = "Thank you for completing the interview {{applicantName}}. Someone will be in touch soon regarding the {{jobTitle}} position.";
}
