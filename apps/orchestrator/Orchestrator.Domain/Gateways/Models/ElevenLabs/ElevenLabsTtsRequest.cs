using System.Text.Json.Serialization;

namespace Orchestrator.Domain;

/// <summary>
/// Request model for ElevenLabs text-to-speech API
/// </summary>
internal class ElevenLabsTtsRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("model_id")]
    public string ModelId { get; set; } = "eleven_monolingual_v1";

    [JsonPropertyName("voice_settings")]
    public ElevenLabsVoiceSettings VoiceSettings { get; set; } = new();
}

/// <summary>
/// Voice settings for ElevenLabs TTS
/// </summary>
internal class ElevenLabsVoiceSettings
{
    [JsonPropertyName("stability")]
    public decimal Stability { get; set; } = 0.5m;

    [JsonPropertyName("similarity_boost")]
    public decimal SimilarityBoost { get; set; } = 0.75m;
}
