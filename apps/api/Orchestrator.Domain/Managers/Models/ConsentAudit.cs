using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Audit record for voice cloning consent (required before IVC).
/// </summary>
[Table("consent_audit")]
public class ConsentAudit
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("persona_id")]
    public Guid PersonaId { get; set; }

    [Column("consent_text_version")]
    public string? ConsentTextVersion { get; set; }

    [Column("attested")]
    public bool Attested { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
