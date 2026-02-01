namespace Orchestrator.Domain;

/// <summary>
/// Result of creating a voice from a sample (IVC).
/// </summary>
public class VoiceCloneResult
{
    public string VoiceId { get; set; } = string.Empty;
    public string VoiceName { get; set; } = string.Empty;
}
