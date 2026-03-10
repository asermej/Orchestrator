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
        int maxTokens)
    {
        var request = new AnthropicMessagesRequest
        {
            Model = model,
            System = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt,
            Temperature = temperature,
            MaxTokens = maxTokens,
            Messages = new List<AnthropicMessage>()
        };

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
