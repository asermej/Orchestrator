using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Orchestrator.Domain;

/// <summary>
/// A single SSE chunk from the OpenAI streaming chat completion API.
/// Each chunk contains a delta with partial content rather than a complete message.
/// </summary>
internal sealed class OpenAIStreamingChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<OpenAIStreamingChoice> Choices { get; set; } = new();
}

internal sealed class OpenAIStreamingChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("delta")]
    public OpenAIStreamingDelta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

internal sealed class OpenAIStreamingDelta
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
