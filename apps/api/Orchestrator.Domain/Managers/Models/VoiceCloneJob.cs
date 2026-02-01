using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Tracks a voice clone job (IVC) for rate limiting and status.
/// </summary>
[Table("voice_clone_jobs")]
public class VoiceCloneJob
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("persona_id")]
    public Guid PersonaId { get; set; }

    [Column("sample_blob_url")]
    public string? SampleBlobUrl { get; set; }

    [Column("sample_duration_seconds")]
    public int? SampleDurationSeconds { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("eleven_labs_voice_id")]
    public string? ElevenLabsVoiceId { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("style_lane")]
    public string? StyleLane { get; set; }
}
