namespace Orchestrator.Domain;

/// <summary>
/// Domain DTO for an ElevenLabs voice (prebuilt or user-cloned) in list responses.
/// </summary>
public class ElevenLabsVoiceItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PreviewText { get; set; }
    public string? Category { get; set; }
    public string VoiceType { get; set; } = "prebuilt"; // "prebuilt" | "user_cloned"
}
