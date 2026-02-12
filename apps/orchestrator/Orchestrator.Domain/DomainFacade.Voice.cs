using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Domain facade partial for voice selection and preview operations.
/// </summary>
public sealed partial class DomainFacade
{
    private VoiceManager? _voiceManager;
    private VoiceManager VoiceManager => _voiceManager ??= new VoiceManager(_serviceLocator);

    /// <summary>
    /// Returns available voices (prebuilt; fake mode returns deterministic list).
    /// </summary>
    public async Task<IReadOnlyList<ElevenLabsVoiceItem>> GetAvailableVoicesAsync(CancellationToken cancellationToken = default)
    {
        return await VoiceManager.GetAvailableVoicesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns curated stock voices from the database (for Choose a voice).
    /// </summary>
    public async Task<IReadOnlyList<StockVoice>> GetStockVoicesAsync(CancellationToken cancellationToken = default)
    {
        return await VoiceManager.GetStockVoicesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the agent's voice to the given prebuilt voice.
    /// </summary>
    public async Task SelectAgentVoiceAsync(Guid agentId, string voiceProvider, string voiceType, string voiceId, string? voiceName, CancellationToken cancellationToken = default)
    {
        await VoiceManager.SelectAgentVoiceAsync(agentId, voiceProvider, voiceType, voiceId, voiceName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Previews a voice by generating a short sample.
    /// </summary>
    public async Task<byte[]> PreviewVoiceAsync(string voiceId, string text, CancellationToken cancellationToken = default)
    {
        return await VoiceManager.PreviewVoiceAsync(voiceId, text, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Previews the agent's current voice by generating a short TTS sample.
    /// </summary>
    public async Task<byte[]> PreviewAgentVoiceAsync(Guid agentId, string text, CancellationToken cancellationToken = default)
    {
        return await VoiceManager.PreviewAgentVoiceAsync(agentId, text, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Streams voice audio chunks as they are generated for low-latency playback.
    /// </summary>
    public async IAsyncEnumerable<byte[]> StreamVoiceAsync(string voiceId, string text, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in VoiceManager.StreamVoiceAsync(voiceId, text, cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Warms up audio cache for interview questions by pre-generating TTS for all question texts.
    /// </summary>
    public async Task<InterviewAudioWarmupResult> WarmupInterviewAudioAsync(Guid interviewId, CancellationToken cancellationToken = default)
    {
        return await VoiceManager.WarmupInterviewAudioAsync(interviewId, cancellationToken).ConfigureAwait(false);
    }
}
