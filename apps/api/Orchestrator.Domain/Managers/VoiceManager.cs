using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Orchestrates voice selection, consent audit, and voice cloning (IVC).
/// </summary>
internal sealed class VoiceManager : IDisposable
{
    private const int MinSampleDurationSeconds = 10;
    private const int MaxSampleDurationSeconds = 300;
    private const int CloneRateLimitHours = 24;
    private const int CloneRateLimitPerPeriod = 5;

    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);
    private PersonaManager? _personaManager;
    private PersonaManager PersonaManager => _personaManager ??= new PersonaManager(_serviceLocator);

    public VoiceManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// Records consent for voice cloning (required before IVC).
    /// </summary>
    public async Task<Guid> RecordConsentAsync(string userId, Guid personaId, string? consentTextVersion, bool attested, CancellationToken cancellationToken = default)
    {
        if (!attested)
        {
            throw new VoiceSampleValidationException("Consent must be attested.");
        }

        var consent = new ConsentAudit
        {
            UserId = userId,
            PersonaId = personaId,
            ConsentTextVersion = consentTextVersion,
            Attested = attested
        };
        var created = await DataFacade.AddConsentAudit(consent).ConfigureAwait(false);
        return created.Id;
    }

    /// <summary>
    /// Returns available voices (prebuilt from ElevenLabs; fake mode returns deterministic list).
    /// </summary>
    public async Task<IReadOnlyList<ElevenLabsVoiceItem>> GetAvailableVoicesAsync(CancellationToken cancellationToken = default)
    {
        var list = await GatewayFacade.ListVoicesAsync(cancellationToken).ConfigureAwait(false);
        return list ?? (IReadOnlyList<ElevenLabsVoiceItem>)new List<ElevenLabsVoiceItem>();
    }

    /// <summary>
    /// Returns curated stock voices from the database (for Choose a voice).
    /// </summary>
    public async Task<IReadOnlyList<StockVoice>> GetStockVoicesAsync(CancellationToken cancellationToken = default)
    {
        return await DataFacade.GetStockVoicesAsync();
    }

    /// <summary>
    /// Uploads voice sample bytes to Azure Blob (voice-samples container) and returns blob path for audit.
    /// </summary>
    public async Task<string> UploadVoiceSampleAsync(byte[] bytes, string fileName, string? contentType, CancellationToken cancellationToken = default)
    {
        var config = _serviceLocator.CreateConfigurationProvider();
        var storage = new VoiceSampleStorageManager(config);
        return await storage.UploadAsync(bytes, fileName, contentType ?? "audio/mpeg", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the persona's voice to the given voice (prebuilt or user-cloned).
    /// </summary>
    public async Task SelectPersonaVoiceAsync(Guid personaId, string voiceProvider, string voiceType, string voiceId, string? voiceName, CancellationToken cancellationToken = default)
    {
        var persona = await PersonaManager.GetPersonaById(personaId).ConfigureAwait(false);
        if (persona == null)
        {
            throw new PersonaNotFoundException($"Persona with ID {personaId} not found.");
        }

        persona.ElevenLabsVoiceId = voiceId;
        persona.VoiceProvider = voiceProvider;
        persona.VoiceType = voiceType;
        persona.VoiceName = voiceName ?? voiceId;
        persona.VoiceCreatedAt = null;
        persona.VoiceCreatedByUserId = null;
        await DataFacade.UpdatePersona(persona).ConfigureAwait(false);
    }

    /// <summary>
    /// Clones a voice from a sample (IVC). Validates consent, rate limit, and sample duration.
    /// </summary>
    /// <param name="sampleBlobUrl">Stored blob URL for audit (optional)</param>
    /// <param name="sampleAudioBytes">Audio bytes to send to ElevenLabs (caller downloads from blob if needed)</param>
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
        var consent = await DataFacade.GetConsentAuditById(consentRecordId).ConfigureAwait(false);
        if (consent == null || consent.UserId != userId || consent.PersonaId != personaId || !consent.Attested)
        {
            throw new ConsentNotFoundException("Consent record not found or does not match.");
        }

        var successCount = await DataFacade.GetSuccessfulCloneCountByUserIdWithinHoursAsync(userId, CloneRateLimitHours).ConfigureAwait(false);
        if (successCount >= CloneRateLimitPerPeriod)
        {
            throw new VoiceCloneRateLimitExceededException($"You can create up to {CloneRateLimitPerPeriod} voices per {CloneRateLimitHours} hours. Please try again later.");
        }

        if (sampleDurationSeconds < MinSampleDurationSeconds)
        {
            throw new VoiceSampleValidationException($"Sample must be at least {MinSampleDurationSeconds} seconds.");
        }

        if (sampleDurationSeconds > MaxSampleDurationSeconds)
        {
            throw new VoiceSampleValidationException($"Sample must be at most {MaxSampleDurationSeconds} seconds.");
        }

        var job = new VoiceCloneJob
        {
            UserId = userId,
            PersonaId = personaId,
            SampleBlobUrl = sampleBlobUrl,
            SampleDurationSeconds = sampleDurationSeconds,
            Status = "Pending",
            StyleLane = styleLane
        };
        job = await DataFacade.AddVoiceCloneJob(job).ConfigureAwait(false);

        try
        {
            var result = await GatewayFacade.CreateVoiceFromSampleAsync(voiceName, sampleAudioBytes, "sample.mp3", cancellationToken).ConfigureAwait(false);

            job.Status = "Success";
            job.ElevenLabsVoiceId = result.VoiceId;
            job.ErrorMessage = null;
            await DataFacade.UpdateVoiceCloneJob(job).ConfigureAwait(false);

            var persona = await PersonaManager.GetPersonaById(personaId).ConfigureAwait(false);
            if (persona != null)
            {
                persona.ElevenLabsVoiceId = result.VoiceId;
                persona.VoiceProvider = "elevenlabs";
                persona.VoiceType = "user_cloned";
                persona.VoiceName = result.VoiceName;
                persona.VoiceCreatedAt = DateTime.UtcNow;
                persona.VoiceCreatedByUserId = userId;
                await DataFacade.UpdatePersona(persona).ConfigureAwait(false);
            }

            return result;
        }
        catch (Exception ex)
        {
            job.Status = "Failed";
            job.ErrorMessage = ex.Message;
            await DataFacade.UpdateVoiceCloneJob(job).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Previews a voice by generating a short sample (uses existing TTS path).
    /// </summary>
    public async Task<byte[]> PreviewVoiceAsync(string voiceId, string text, CancellationToken cancellationToken = default)
    {
        var config = GatewayFacade.GetElevenLabsConfig();
        if (!config.Enabled && !config.UseFakeElevenLabs)
        {
            throw new ElevenLabsDisabledException();
        }

        return await GatewayFacade.GenerateSpeechAsync(text, voiceId, 0.5m, 0.75m, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Previews the persona's current voice by generating a short TTS sample (uses existing TTS path).
    /// </summary>
    public async Task<byte[]> PreviewPersonaVoiceAsync(Guid personaId, string text, CancellationToken cancellationToken = default)
    {
        var persona = await PersonaManager.GetPersonaById(personaId).ConfigureAwait(false);
        if (persona == null)
        {
            throw new PersonaNotFoundException($"Persona with ID {personaId} not found.");
        }

        var config = GatewayFacade.GetElevenLabsConfig();
        var voiceId = persona.ElevenLabsVoiceId ?? config.DefaultVoiceId;
        return await PreviewVoiceAsync(voiceId, text, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _gatewayFacade?.Dispose();
        _personaManager?.Dispose();
    }
}
