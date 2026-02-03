using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a Topic in the domain
/// </summary>
[Table("topics")]
public class Topic : Entity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("agent_id")]
    public Guid AgentId { get; set; }

    [Column("content_url")]
    public string ContentUrl { get; set; } = string.Empty;

    [Column("contribution_notes")]
    public string? ContributionNotes { get; set; }
}

