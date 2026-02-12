using System.Collections.Generic;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Maps between domain models and OpenAI API resource models
/// </summary>
internal static class OpenAIMapper
{
    /// <summary>
    /// Creates an OpenAI chat completion request from system prompt, chat history, and user message
    /// </summary>
    /// <param name="systemPrompt">The system prompt that defines the persona behavior</param>
    /// <param name="chatHistory">Previous messages in the conversation</param>
    /// <param name="model">The OpenAI model to use (e.g., gpt-3.5-turbo)</param>
    /// <param name="temperature">Temperature setting for response randomness</param>
    /// <param name="maxTokens">Maximum tokens in the response</param>
    /// <returns>OpenAI API request object</returns>
    public static OpenAIChatCompletionRequest ToChatCompletionRequest(
        string systemPrompt,
        IEnumerable<ConversationTurn> chatHistory,
        string model,
        double temperature,
        int maxTokens)
    {
        var request = new OpenAIChatCompletionRequest
        {
            Model = model,
            Temperature = temperature,
            MaxTokens = maxTokens,
            Messages = new List<OpenAIChatMessage>()
        };

        // Add system prompt first
        request.Messages.Add(new OpenAIChatMessage
        {
            Role = "system",
            Content = systemPrompt
        });

        // Add chat history
        foreach (var turn in chatHistory)
        {
            request.Messages.Add(new OpenAIChatMessage
            {
                Role = turn.Role,
                Content = turn.Content
            });
        }

        return request;
    }

    /// <summary>
    /// Extracts the assistant's response content from OpenAI API response
    /// </summary>
    /// <param name="response">The OpenAI API response</param>
    /// <returns>The assistant's message content</returns>
    public static string ExtractResponseContent(OpenAIChatCompletionResponse response)
    {
        if (response.Choices == null || response.Choices.Count == 0)
        {
            throw new OpenAIApiException("OpenAI API response contains no choices");
        }

        var firstChoice = response.Choices[0];
        if (firstChoice.Message == null || string.IsNullOrWhiteSpace(firstChoice.Message.Content))
        {
            throw new OpenAIApiException("OpenAI API response message content is empty");
        }

        return firstChoice.Message.Content;
    }
}

