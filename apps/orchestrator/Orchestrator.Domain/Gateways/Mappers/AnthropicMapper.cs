using System.Collections.Generic;
using System.Linq;

namespace Orchestrator.Domain;

internal static class AnthropicMapper
{
    /// <summary>
    /// Creates an Anthropic messages request. The system prompt is placed in the
    /// top-level "system" field (not as a message), per the Anthropic API contract.
    /// </summary>
    public static AnthropicMessagesRequest ToMessagesRequest(
        string systemPrompt,
        IEnumerable<ConversationTurn> chatHistory,
        string model,
        double temperature,
        int maxTokens,
        bool enablePromptCaching = false)
    {
        return ToMessagesRequest(
            systemPrompt, systemPromptInterviewPart: null,
            chatHistory, model, temperature, maxTokens, enablePromptCaching);
    }

    /// <summary>
    /// Creates an Anthropic messages request with optional two-block prompt caching.
    /// When both parts are provided and caching is enabled, emits two system blocks:
    /// block 0 = static rules (shared across all interviews, cached as a stable prefix),
    /// block 1 = per-interview context (agent identity, candidate, role).
    /// Both blocks carry cache_control=ephemeral so Anthropic caches the prefix up to
    /// each breakpoint independently.
    /// </summary>
    public static AnthropicMessagesRequest ToMessagesRequest(
        string systemPrompt,
        string? systemPromptInterviewPart,
        IEnumerable<ConversationTurn> chatHistory,
        string model,
        double temperature,
        int maxTokens,
        bool enablePromptCaching = false)
    {
        var request = new AnthropicMessagesRequest
        {
            Model = model,
            Temperature = temperature,
            MaxTokens = maxTokens,
            Messages = new List<AnthropicMessage>()
        };

        if (enablePromptCaching && !string.IsNullOrWhiteSpace(systemPrompt))
        {
            var blocks = new List<AnthropicSystemBlock>
            {
                new AnthropicSystemBlock
                {
                    Type = "text",
                    Text = systemPrompt,
                    CacheControl = new AnthropicCacheControl { Type = "ephemeral" }
                }
            };

            if (!string.IsNullOrWhiteSpace(systemPromptInterviewPart))
            {
                blocks.Add(new AnthropicSystemBlock
                {
                    Type = "text",
                    Text = systemPromptInterviewPart,
                    CacheControl = new AnthropicCacheControl { Type = "ephemeral" }
                });
            }

            request.System = blocks;
        }
        else if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            var combined = systemPromptInterviewPart != null
                ? systemPrompt + systemPromptInterviewPart
                : systemPrompt;
            request.System = combined;
        }

        foreach (var turn in chatHistory)
        {
            request.Messages.Add(new AnthropicMessage
            {
                Role = turn.Role,
                Content = turn.Content
            });
        }

        return request;
    }

    /// <summary>
    /// Extracts the text content from the first text content block in the response.
    /// </summary>
    public static string ExtractResponseContent(AnthropicMessagesResponse response)
    {
        if (response.Content == null || response.Content.Count == 0)
        {
            throw new AnthropicApiException("Anthropic API response contains no content blocks");
        }

        var textBlock = response.Content.FirstOrDefault(c => c.Type == "text");
        if (textBlock == null || string.IsNullOrWhiteSpace(textBlock.Text))
        {
            throw new AnthropicApiException("Anthropic API response contains no text content");
        }

        return textBlock.Text;
    }
}
