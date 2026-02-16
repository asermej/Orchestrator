using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a Group (tenant) in the domain.
/// Named "Group" conceptually - maps to the "groups" table (formerly "organizations").
/// </summary>
[Table("groups")]
public class Group : Entity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    [Column("webhook_url")]
    public string? WebhookUrl { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("external_group_id")]
    public Guid? ExternalGroupId { get; set; }

    [Column("ats_base_url")]
    public string? AtsBaseUrl { get; set; }

    [Column("ats_api_key")]
    public string? AtsApiKey { get; set; }
}
