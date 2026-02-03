using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Tracks a webhook delivery attempt
/// </summary>
[Table("webhook_deliveries")]
public class WebhookDelivery
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("webhook_config_id")]
    public Guid WebhookConfigId { get; set; }

    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [Column("payload")]
    public string Payload { get; set; } = string.Empty;

    [Column("response_status_code")]
    public int? ResponseStatusCode { get; set; }

    [Column("response_body")]
    public string? ResponseBody { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("status")]
    public string Status { get; set; } = WebhookDeliveryStatus.Pending;

    [Column("attempts")]
    public int Attempts { get; set; } = 0;

    [Column("next_retry_at")]
    public DateTime? NextRetryAt { get; set; }

    [Column("delivered_at")]
    public DateTime? DeliveredAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Webhook delivery status values
/// </summary>
public static class WebhookDeliveryStatus
{
    public const string Pending = "pending";
    public const string Delivered = "delivered";
    public const string Failed = "failed";
    public const string Retrying = "retrying";
}
