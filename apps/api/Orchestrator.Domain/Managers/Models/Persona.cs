using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a Persona in the domain
/// </summary>
[Table("personas")]
public class Persona : Entity
{
    [Column("first_name")]
    public string? FirstName { get; set; }

    [Column("last_name")]
    public string? LastName { get; set; }

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("profile_image_url")]
    public string? ProfileImageUrl { get; set; }

    [Column("training_file_path")]
    public string? TrainingFilePath { get; set; }

    [Column("elevenlabs_voice_id")]
    public string? ElevenLabsVoiceId { get; set; }

    [Column("voice_stability")]
    public decimal VoiceStability { get; set; } = 0.50m;

    [Column("voice_similarity_boost")]
    public decimal VoiceSimilarityBoost { get; set; } = 0.75m;

    [Column("voice_provider")]
    public string? VoiceProvider { get; set; }

    [Column("voice_type")]
    public string? VoiceType { get; set; }

    [Column("voice_name")]
    public string? VoiceName { get; set; }

    [Column("voice_created_at")]
    public DateTime? VoiceCreatedAt { get; set; }

    [Column("voice_created_by_user_id")]
    public string? VoiceCreatedByUserId { get; set; }
}

