using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
    /// Generates a chat completion using OpenAI's GPT model.
    /// Optional model/temperature overrides allow per-call tuning (e.g. faster model for classification).
    /// </summary>
    public async Task<string> GenerateChatCompletion(
        string systemPrompt,
        IEnumerable<ConversationTurn> chatHistory,
        string? modelOverride = null,
        double? temperatureOverride = null)
    {
        return await OpenAIManager.GenerateChatCompletion(systemPrompt, chatHistory, modelOverride, temperatureOverride).ConfigureAwait(false);
    }

    /// <summary>
    /// Streams a chat completion token-by-token from OpenAI.
    /// Used by the phone call pipeline to feed sentences to TTS as they arrive.
    /// </summary>
    public async IAsyncEnumerable<string> StreamChatCompletionAsync(
        string systemPrompt,
        IEnumerable<ConversationTurn> chatHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var token in OpenAIManager.StreamChatCompletionAsync(systemPrompt, chatHistory, cancellationToken).ConfigureAwait(false))
        {
            yield return token;
        }
    }
}

