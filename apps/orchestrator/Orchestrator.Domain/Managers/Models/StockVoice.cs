using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Curated stock voice (ElevenLabs prebuilt) for Choose a voice.
/// </summary>
[Table("stock_voices")]
public class StockVoice
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("voice_id")]
    public string VoiceId { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("tags")]
    public string? Tags { get; set; }

    [Column("preview_text")]
    public string? PreviewText { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }
}
