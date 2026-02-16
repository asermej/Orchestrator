using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Configuration for a webhook endpoint that receives event notifications
/// </summary>
[Table("webhook_configs")]
public class WebhookConfig : Entity
{
    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("url")]
    public string Url { get; set; } = string.Empty;

    [Column("secret")]
    public string? Secret { get; set; }

    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Webhook event types
/// </summary>
public static class WebhookEventTypes
{
    public const string InterviewCompleted = "interview.completed";
    public const string InterviewStarted = "interview.started";
    public const string InterviewCancelled = "interview.cancelled";
}
