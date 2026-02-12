using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents an Organization (tenant) in the domain
/// </summary>
[Table("organizations")]
public class Organization : Entity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    [Column("webhook_url")]
    public string? WebhookUrl { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
