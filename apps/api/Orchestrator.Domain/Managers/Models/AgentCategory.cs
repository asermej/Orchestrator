using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents an AgentCategory junction in the domain
/// </summary>
[Table("agent_categories")]
public class AgentCategory
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("agent_id")]
    public Guid AgentId { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

