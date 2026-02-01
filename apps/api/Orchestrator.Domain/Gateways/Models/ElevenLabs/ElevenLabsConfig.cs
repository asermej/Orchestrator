namespace Orchestrator.Domain;

/// <summary>
/// Configuration settings for ElevenLabs gateway
/// </summary>
internal class ElevenLabsConfig
{
    public bool Enabled { get; set; }
    public bool UseFakeElevenLabs { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.elevenlabs.io";
    public string DefaultVoiceId { get; set; } = "21m00Tcm4TlvDq8ikWAM";
    public string ModelId { get; set; } = "eleven_monolingual_v1";
    public int MaxCharsPerRequest { get; set; } = 500;
    public int MaxRequestsPerMessage { get; set; } = 6;
}
