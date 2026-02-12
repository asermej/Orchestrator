using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Gateway facade partial class for OpenAI integration
/// </summary>
internal sealed partial class GatewayFacade
{
    private OpenAIManager? _openAIManager;
    private OpenAIManager OpenAIManager => _openAIManager ??= new OpenAIManager(_serviceLocator);

    /// <summary>
    /// Generates a chat completion using OpenAI's GPT model
    /// </summary>
    /// <param name="systemPrompt">The system prompt that defines the persona behavior</param>
    /// <param name="chatHistory">Previous messages in the conversation</param>
    /// <returns>The AI-generated response content</returns>
    public async Task<string> GenerateChatCompletion(string systemPrompt, IEnumerable<ConversationTurn> chatHistory)
    {
        return await OpenAIManager.GenerateChatCompletion(systemPrompt, chatHistory).ConfigureAwait(false);
    }
}

