using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

internal sealed partial class GatewayFacade
{
    private AnthropicManager? _anthropicManager;
    private AnthropicManager AnthropicManager => _anthropicManager ??= new AnthropicManager(_serviceLocator);

    public async Task<string> GenerateAnthropicCompletion(
        string systemPrompt,
        IEnumerable<ConversationTurn> chatHistory,
        string? modelOverride = null,
        double? temperatureOverride = null,
        int? maxTokensOverride = null)
    {
        return await AnthropicManager.GenerateCompletion(systemPrompt, chatHistory, modelOverride, temperatureOverride, maxTokensOverride).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<string> StreamAnthropicCompletionAsync(
        string systemPrompt,
        IEnumerable<ConversationTurn> chatHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var token in AnthropicManager.StreamCompletionAsync(systemPrompt, chatHistory, cancellationToken).ConfigureAwait(false))
        {
            yield return token;
        }
    }
}
