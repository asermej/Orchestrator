using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Orchestrator.Domain;

/// <summary>
/// Request model for OpenAI Chat Completion API
/// </summary>
internal sealed class OpenAIChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OpenAIChatMessage> Messages { get; set; } = new List<OpenAIChatMessage>();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }
}

/// <summary>
/// Represents a chat message in OpenAI format
/// </summary>
internal sealed class OpenAIChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

