using System.Text.Json;
using System.Text.Json.Serialization;

namespace Orchestrator.Domain;

/// <summary>
/// Initialization message sent when opening an ElevenLabs TTS WebSocket stream.
/// </summary>
internal sealed class ElevenLabsTtsWsInitMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = " ";

    [JsonPropertyName("voice_settings")]
    public ElevenLabsTtsWsVoiceSettings VoiceSettings { get; set; } = new();

    [JsonPropertyName("generation_config")]
    public ElevenLabsTtsWsGenerationConfig GenerationConfig { get; set; } = new();

    [JsonPropertyName("xi-api-key")]
    public string ApiKey { get; set; } = string.Empty;
}

internal sealed class ElevenLabsTtsWsVoiceSettings
{
    [JsonPropertyName("stability")]
    public double Stability { get; set; } = 0.5;

    [JsonPropertyName("similarity_boost")]
    public double SimilarityBoost { get; set; } = 0.75;

    [JsonPropertyName("style")]
    public int Style { get; set; }

    [JsonPropertyName("use_speaker_boost")]
    public bool UseSpeakerBoost { get; set; } = true;

    [JsonPropertyName("speed")]
    public double Speed { get; set; } = 1.0;
}

internal sealed class ElevenLabsTtsWsGenerationConfig
{
    [JsonPropertyName("chunk_length_schedule")]
    public int[] ChunkLengthSchedule { get; set; } = [80, 120, 200, 290];
}

/// <summary>
/// Text message sent to ElevenLabs TTS WebSocket to synthesize speech.
/// </summary>
internal sealed class ElevenLabsTtsWsTextMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("try_trigger_generation")]
    public bool TryTriggerGeneration { get; set; } = true;

    [JsonPropertyName("flush")]
    public bool Flush { get; set; } = true;
}

/// <summary>
/// Close/flush signal sent to ElevenLabs TTS WebSocket after all text is sent.
/// </summary>
internal sealed class ElevenLabsTtsWsCloseMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

/// <summary>
/// Audio response chunk received from ElevenLabs TTS WebSocket.
/// </summary>
internal sealed class ElevenLabsTtsWsAudioResponse
{
    [JsonPropertyName("audio")]
    public string? Audio { get; set; }

    [JsonPropertyName("isFinal")]
    public JsonElement? IsFinalRaw { get; set; }

    [JsonIgnore]
    public bool IsFinal => IsFinalRaw?.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => IsFinalRaw.Value.GetInt32() != 0,
        JsonValueKind.Null => false,
        _ => false
    };
}
