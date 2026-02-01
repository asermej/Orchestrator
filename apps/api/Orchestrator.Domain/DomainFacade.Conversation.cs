using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Orchestrator.Domain;

/// <summary>
/// Domain facade partial class for voice conversation operations
/// </summary>
public sealed partial class DomainFacade
{
    private ConversationManager? _conversationManager;
    private ConversationManager ConversationManager => _conversationManager ??= new ConversationManager(_serviceLocator);

    /// <summary>
    /// Streams audio response for a voice conversation.
    /// Saves user message, generates AI response, and streams TTS audio.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="personaId">The persona ID for voice settings</param>
    /// <param name="userMessage">The user's text message (transcribed from speech)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of audio chunks (MP3 bytes)</returns>
    public async IAsyncEnumerable<byte[]> StreamAudioResponseAsync(
        Guid chatId,
        Guid personaId,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ConversationManager.StreamAudioResponseAsync(chatId, personaId, userMessage, cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Checks if ElevenLabs TTS is enabled
    /// </summary>
    public bool IsVoiceEnabled()
    {
        return ConversationManager != null && new GatewayFacade(_serviceLocator).IsElevenLabsEnabled();
    }
}
