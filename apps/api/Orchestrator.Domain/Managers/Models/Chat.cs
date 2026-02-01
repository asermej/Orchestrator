using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a Chat in the domain
/// </summary>
[Table("chats")]
public class Chat : Entity
{
    [Column("persona_id")]
    public Guid PersonaId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("last_message_at")]
    public DateTime LastMessageAt { get; set; }
}

