using System.Text.Json.Serialization;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a message on the Telnyx bidirectional media stream WebSocket.
/// </summary>
internal sealed class TelnyxMediaMessage
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("stream_id")]
    public string? StreamId { get; set; }

    [JsonPropertyName("media")]
    public TelnyxMediaPayload? Media { get; set; }
}

internal sealed class TelnyxMediaPayload
{
    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;
}
