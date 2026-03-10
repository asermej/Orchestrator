using System.Text.Json.Serialization;

namespace Orchestrator.Domain;

/// <summary>
/// Outbound message sent to ElevenLabs realtime STT WebSocket.
/// </summary>
internal sealed class ElevenLabsSttAudioChunk
{
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = "input_audio_chunk";

    [JsonPropertyName("audio_base_64")]
    public string AudioBase64 { get; set; } = string.Empty;

    [JsonPropertyName("sample_rate")]
    public int SampleRate { get; set; } = 8000;

    [JsonPropertyName("commit")]
    public bool Commit { get; set; }
}

/// <summary>
/// Inbound message received from ElevenLabs realtime STT WebSocket.
/// Covers session_started, partial_transcript, committed_transcript, and error types.
/// </summary>
internal sealed class ElevenLabsSttResponse
{
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
