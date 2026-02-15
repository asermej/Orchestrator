using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents an interview invite link in the domain.
/// Maps a short code to an interview for candidate access.
/// </summary>
[Table("interview_invites")]
public class InterviewInvite : Entity
{
    [Column("interview_id")]
    public Guid InterviewId { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("short_code")]
    public string ShortCode { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = InviteStatus.Active;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("max_uses")]
    public int MaxUses { get; set; } = 3;

    [Column("use_count")]
    public int UseCount { get; set; } = 0;

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("revoked_by")]
    public string? RevokedBy { get; set; }
}

/// <summary>
/// Interview invite status constants
/// </summary>
public static class InviteStatus
{
    public const string Active = "active";
    public const string Consumed = "consumed";
    public const string Revoked = "revoked";
    public const string Expired = "expired";
}
