using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a ChatTopic in the domain - links chats to topics for conversation context
/// </summary>
[Table("chat_topics")]
public class ChatTopic
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("chat_id")]
    public Guid ChatId { get; set; }

    [Column("topic_id")]
    public Guid TopicId { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; }
}

