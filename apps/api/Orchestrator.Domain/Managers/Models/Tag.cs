using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a Tag in the domain
/// </summary>
[Table("tags")]
public class Tag : Entity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;
}

