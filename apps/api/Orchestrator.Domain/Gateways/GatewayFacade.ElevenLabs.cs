using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Gateway facade partial class for ElevenLabs TTS integration
/// </summary>
internal sealed partial class GatewayFacade
{
    private ElevenLabsManager? _elevenLabsManager;
    private ElevenLabsManager ElevenLabsManager => _elevenLabsManager ??= new ElevenLabsManager(_serviceLocator);

    /// <summary>
    /// Checks if ElevenLabs TTS is enabled in configuration
    /// </summary>
    public bool IsElevenLabsEnabled()
    {
        return ElevenLabsManager.IsEnabled();
    }

    /// <summary>
    /// Gets the ElevenLabs configuration settings
    /// </summary>
    public ElevenLabsConfig GetElevenLabsConfig()
    {
        return ElevenLabsManager.GetConfig();
    }

    /// <summary>
    /// Streams speech audio from text using ElevenLabs TTS.
    /// Returns audio chunks as they arrive for immediate playback.
    /// </summary>
    /// <param name="text">Text to convert to speech</param>
    /// <param name="voiceId">ElevenLabs voice ID (uses default if null)</param>
    /// <param name="stability">Voice stability (0.0-1.0)</param>
    /// <param name="similarityBoost">Voice similarity boost (0.0-1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of audio chunks (MP3 bytes)</returns>
    public async IAsyncEnumerable<byte[]> StreamSpeechAsync(
        string text,
        string? voiceId = null,
        decimal stability = 0.5m,
        decimal similarityBoost = 0.75m,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ElevenLabsManager.StreamSpeechAsync(text, voiceId, stability, similarityBoost, cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Generates complete speech audio from text (non-streaming).
    /// Use this for caching scenarios where you need the complete audio.
    /// </summary>
    /// <param name="text">Text to convert to speech</param>
    /// <param name="voiceId">ElevenLabs voice ID (uses default if null)</param>
    /// <param name="stability">Voice stability (0.0-1.0)</param>
    /// <param name="similarityBoost">Voice similarity boost (0.0-1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete audio as byte array (MP3)</returns>
    public async Task<byte[]> GenerateSpeechAsync(
        string text,
        string? voiceId = null,
        decimal stability = 0.5m,
        decimal similarityBoost = 0.75m,
        CancellationToken cancellationToken = default)
    {
        return await ElevenLabsManager.GenerateSpeechAsync(text, voiceId, stability, similarityBoost, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists available ElevenLabs voices (prebuilt; fake mode returns deterministic list).
    /// </summary>
    public async Task<IReadOnlyList<ElevenLabsVoiceItem>> ListVoicesAsync(CancellationToken cancellationToken = default)
    {
        return await ElevenLabsManager.ListVoicesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a voice from a sample (Instant Voice Cloning). Caller must provide sample bytes (e.g. from blob).
    /// </summary>
    public async Task<VoiceCloneResult> CreateVoiceFromSampleAsync(string voiceName, byte[] sampleAudioBytes, string fileName = "sample.mp3", CancellationToken cancellationToken = default)
    {
        return await ElevenLabsManager.CreateVoiceFromSampleAsync(voiceName, sampleAudioBytes, fileName, cancellationToken).ConfigureAwait(false);
    }
}
