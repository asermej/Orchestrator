using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a PersonaCategory junction in the domain
/// </summary>
[Table("persona_categories")]
public class PersonaCategory
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("persona_id")]
    public Guid PersonaId { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

