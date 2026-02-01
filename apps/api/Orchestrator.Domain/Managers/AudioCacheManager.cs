using Orchestrator.Domain.DataLayer;

namespace Orchestrator.Domain;

/// <summary>
/// Manages audio caching operations for TTS replay.
/// Checks cache first, generates if needed, then caches for future use.
/// </summary>
internal sealed class AudioCacheManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private AudioCacheDataManager? _audioCacheDataManager;
    private AudioCacheDataManager AudioCacheDataManager => _audioCacheDataManager ??= 
        new AudioCacheDataManager(_serviceLocator.CreateConfigurationProvider());
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);

    public AudioCacheManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// Gets or generates audio for a message.
    /// Checks cache first, generates via ElevenLabs if not cached, then caches for future.
    /// </summary>
    /// <param name="messageText">The message text to convert to speech</param>
    /// <param name="voiceId">ElevenLabs voice ID</param>
    /// <param name="stability">Voice stability setting</param>
    /// <param name="similarityBoost">Voice similarity boost setting</param>
    /// <returns>Audio bytes (MP3)</returns>
    public async Task<byte[]> GetOrGenerateAudioAsync(
        string messageText,
        string voiceId,
        decimal stability,
        decimal similarityBoost)
    {
        var config = GatewayFacade.GetElevenLabsConfig();
        
        if (!config.Enabled)
        {
            throw new ElevenLabsDisabledException("ElevenLabs TTS is disabled in configuration");
        }

        // Generate cache key
        var cacheKey = AudioCacheDataManager.GenerateCacheKey(
            voiceId,
            config.ModelId,
            stability,
            similarityBoost,
            messageText);

        // Check cache
        var cached = await AudioCacheDataManager.GetAsync(cacheKey).ConfigureAwait(false);
        if (cached != null)
        {
            // Return cached audio
            using var memoryStream = new MemoryStream();
            await cached.Stream.CopyToAsync(memoryStream).ConfigureAwait(false);
            return memoryStream.ToArray();
        }

        // Generate new audio
        var audioData = await GatewayFacade.GenerateSpeechAsync(
            messageText,
            voiceId,
            stability,
            similarityBoost).ConfigureAwait(false);

        // Cache for future use
        var metadata = new Dictionary<string, string>
        {
            { "voiceId", voiceId },
            { "modelId", config.ModelId },
            { "stability", stability.ToString("F2") },
            { "similarityBoost", similarityBoost.ToString("F2") },
            { "textLength", messageText.Length.ToString() },
            { "generatedAt", DateTime.UtcNow.ToString("O") }
        };

        await AudioCacheDataManager.SaveAsync(cacheKey, audioData, metadata).ConfigureAwait(false);

        return audioData;
    }

    /// <summary>
    /// Checks if audio is cached for the given parameters.
    /// </summary>
    public async Task<bool> IsCachedAsync(
        string messageText,
        string voiceId,
        decimal stability,
        decimal similarityBoost)
    {
        var config = GatewayFacade.GetElevenLabsConfig();
        
        var cacheKey = AudioCacheDataManager.GenerateCacheKey(
            voiceId,
            config.ModelId,
            stability,
            similarityBoost,
            messageText);

        return await AudioCacheDataManager.ExistsAsync(cacheKey).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _gatewayFacade?.Dispose();
    }
}
