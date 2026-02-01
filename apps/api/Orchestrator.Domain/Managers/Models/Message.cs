using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a Message in the domain
/// </summary>
[Table("messages")]
public class Message : Entity
{
    [Column("chat_id")]
    public Guid ChatId { get; set; }

    [Column("role")]
    public string Role { get; set; } = string.Empty;

    [Column("content")]
    public string Content { get; set; } = string.Empty;
}

