using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents an AI Interviewer Agent in the domain
/// </summary>
[Table("agents")]
public class Agent : Entity
{
    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("profile_image_url")]
    public string? ProfileImageUrl { get; set; }

    [Column("system_prompt")]
    public string? SystemPrompt { get; set; }

    [Column("interview_guidelines")]
    public string? InterviewGuidelines { get; set; }

    [Column("elevenlabs_voice_id")]
    public string? ElevenlabsVoiceId { get; set; }

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

    [Column("visibility_scope")]
    public string VisibilityScope { get; set; } = AgentVisibilityScope.OrganizationOnly;
}
