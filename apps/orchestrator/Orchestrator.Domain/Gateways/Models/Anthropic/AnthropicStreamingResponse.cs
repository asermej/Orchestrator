using System.Text.Json.Serialization;

namespace Orchestrator.Domain;

/// <summary>
/// SSE event envelope. The "type" field determines which nested object is populated:
/// "content_block_delta" -> Delta contains the text token.
/// </summary>
internal sealed class AnthropicStreamingEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("delta")]
    public AnthropicStreamingDelta? Delta { get; set; }
}

internal sealed class AnthropicStreamingDelta
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
