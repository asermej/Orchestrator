using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents an audit log entry for interview-related events.
/// This is an append-only log with no soft delete.
/// </summary>
[Table("interview_audit_logs")]
public class InterviewAuditLog
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("interview_id")]
    public Guid InterviewId { get; set; }

    [Column("invite_id")]
    public Guid? InviteId { get; set; }

    [Column("session_id")]
    public Guid? SessionId { get; set; }

    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [Column("event_data")]
    public string? EventData { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Audit event type constants for interview events
/// </summary>
public static class AuditEventType
{
    public const string InviteCreated = "invite_created";
    public const string InviteRedeemed = "invite_redeemed";
    public const string SessionCreated = "session_created";
    public const string InterviewStarted = "interview_started";
    public const string ResponseSubmitted = "response_submitted";
    public const string InterviewCompleted = "interview_completed";
    public const string InviteRevoked = "invite_revoked";
    public const string SessionExpired = "session_expired";
}
