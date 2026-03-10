using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    private InterviewConversationManager? _interviewConversationManager;
    private InterviewConversationManager InterviewConversationManager =>
        _interviewConversationManager ??= new InterviewConversationManager(_serviceLocator);

    /// <summary>
    /// LATENCY-CRITICAL: Generates a conversational AI response for a candidate's interview turn
    /// and streams it back as MP3 audio chunks. Replaces the sequential classify → evaluate → TTS
    /// round trips with a single streaming pipeline.
    /// </summary>
    public async IAsyncEnumerable<byte[]> RespondToTurnAsync(
        InterviewRuntimeContext context,
        RespondToTurnRequest request,
        Action<TurnResponseMetadata> onMetadataReady,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in InterviewConversationManager.RespondToTurnAsync(
            context, request, onMetadataReady, cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }
}
