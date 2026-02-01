using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Domain facade partial for voice selection and cloning operations.
/// </summary>
public sealed partial class DomainFacade
{
    private VoiceManager? _voiceManager;
    private VoiceManager VoiceManager => _voiceManager ??= new VoiceManager(_serviceLocator);

    /// <summary>
    /// Records consent for voice cloning (required before IVC).
    /// </summary>
    public async Task<Guid> RecordConsentAsync(string userId, Guid personaId, string? consentTextVersion, bool attested, CancellationToken cancellationToken = default)
    {
        return await VoiceManager.RecordConsentAsync(userId, personaId, consentTextVersion, attested, cancellationToken).ConfigureAwait(false);
    }

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
    /// Uploads voice sample bytes to Azure Blob (voice-samples container) and returns blob path for audit.
    /// </summary>
    public async Task<string> UploadVoiceSampleAsync(byte[] bytes, string fileName, string? contentType, CancellationToken cancellationToken = default)
    {
        return await VoiceManager.UploadVoiceSampleAsync(bytes, fileName, contentType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the persona's voice to the given voice.
    /// </summary>
    public async Task SelectPersonaVoiceAsync(Guid personaId, string voiceProvider, string voiceType, string voiceId, string? voiceName, CancellationToken cancellationToken = default)
    {
        await VoiceManager.SelectPersonaVoiceAsync(personaId, voiceProvider, voiceType, voiceId, voiceName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Clones a voice from a sample (IVC). Validates consent, rate limit, and sample duration.
    /// </summary>
    public async Task<VoiceCloneResult> CloneVoiceAsync(
        string userId,
        Guid personaId,
        string voiceName,
        string? sampleBlobUrl,
        byte[] sampleAudioBytes,
        int sampleDurationSeconds,
        Guid consentRecordId,
        string? styleLane = null,
        CancellationToken cancellationToken = default)
    {
        return await VoiceManager.CloneVoiceAsync(userId, personaId, voiceName, sampleBlobUrl, sampleAudioBytes, sampleDurationSeconds, consentRecordId, styleLane, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Previews a voice by generating a short sample.
    /// </summary>
    public async Task<byte[]> PreviewVoiceAsync(string voiceId, string text, CancellationToken cancellationToken = default)
    {
        return await VoiceManager.PreviewVoiceAsync(voiceId, text, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Previews the persona's current voice by generating a short TTS sample.
    /// </summary>
    public async Task<byte[]> PreviewPersonaVoiceAsync(Guid personaId, string text, CancellationToken cancellationToken = default)
    {
        return await VoiceManager.PreviewPersonaVoiceAsync(personaId, text, cancellationToken).ConfigureAwait(false);
    }
}
