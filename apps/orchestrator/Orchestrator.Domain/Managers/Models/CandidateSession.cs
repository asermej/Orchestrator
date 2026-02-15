using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents an active candidate session during an interview.
/// Each session is bound to a JWT via the jti claim.
/// </summary>
[Table("candidate_sessions")]
public class CandidateSession : Entity
{
    [Column("invite_id")]
    public Guid InviteId { get; set; }

    [Column("interview_id")]
    public Guid InterviewId { get; set; }

    [Column("jti")]
    public string Jti { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; }

    [Column("last_activity_at")]
    public DateTime? LastActivityAt { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }
}
