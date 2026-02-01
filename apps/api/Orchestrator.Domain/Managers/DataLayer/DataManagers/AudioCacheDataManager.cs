using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Security.Cryptography;
using System.Text;

namespace Orchestrator.Domain.DataLayer;

/// <summary>
/// Data manager responsible for audio cache storage operations using Azure Blob Storage.
/// Stores generated TTS audio to avoid regenerating for the same message.
/// </summary>
internal sealed class AudioCacheDataManager
{
    private readonly BlobContainerClient _containerClient;
    private const string AudioPrefix = "audio/";
    private const string AudioApiBasePath = "/api/v1/message";

    public AudioCacheDataManager(ConfigurationProviderBase configurationProvider)
    {
        var connectionString = configurationProvider.GetBlobConnectionString();
        var containerName = configurationProvider.GetBlobContainerName();
        
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        
        // Ensure container exists (creates if not present)
        _containerClient.CreateIfNotExists();
    }

    /// <summary>
    /// Generates a deterministic cache key for audio based on voice settings and text.
    /// </summary>
    /// <param name="voiceId">ElevenLabs voice ID</param>
    /// <param name="modelId">ElevenLabs model ID</param>
    /// <param name="stability">Voice stability setting</param>
    /// <param name="similarityBoost">Voice similarity boost setting</param>
    /// <param name="text">Text content to be converted to speech</param>
    /// <returns>SHA256 hash as lowercase hex string</returns>
    public static string GenerateCacheKey(
        string voiceId,
        string modelId,
        decimal stability,
        decimal similarityBoost,
        string text)
    {
        var normalized = text.Trim().ToLowerInvariant();
        var input = $"{voiceId}|{modelId}|{stability:F2}|{similarityBoost:F2}|mp3|{normalized}";
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Checks if audio exists in cache.
    /// </summary>
    /// <param name="cacheKey">The cache key</param>
    /// <returns>True if cached audio exists</returns>
    public async Task<bool> ExistsAsync(string cacheKey)
    {
        var blobClient = _containerClient.GetBlobClient($"{AudioPrefix}{cacheKey}.mp3");
        return await blobClient.ExistsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Saves audio to cache.
    /// </summary>
    /// <param name="cacheKey">The cache key</param>
    /// <param name="audioData">The audio bytes (MP3)</param>
    /// <param name="metadata">Optional metadata for debugging</param>
    public async Task SaveAsync(string cacheKey, byte[] audioData, Dictionary<string, string>? metadata = null)
    {
        try
        {
            var blobPath = $"{AudioPrefix}{cacheKey}.mp3";
            var blobClient = _containerClient.GetBlobClient(blobPath);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = "audio/mpeg",
                CacheControl = "public, max-age=86400" // 24 hour cache
            };

            using var stream = new MemoryStream(audioData);
            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders,
                Metadata = metadata
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new AudioCacheException($"Failed to save audio to cache: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves audio from cache.
    /// </summary>
    /// <param name="cacheKey">The cache key</param>
    /// <returns>Audio download result, or null if not found</returns>
    public async Task<AudioCacheResult?> GetAsync(string cacheKey)
    {
        try
        {
            var blobPath = $"{AudioPrefix}{cacheKey}.mp3";
            var blobClient = _containerClient.GetBlobClient(blobPath);
            
            if (!await blobClient.ExistsAsync().ConfigureAwait(false))
            {
                return null;
            }

            var response = await blobClient.DownloadStreamingAsync().ConfigureAwait(false);

            return new AudioCacheResult
            {
                Stream = response.Value.Content,
                ContentType = "audio/mpeg",
                CacheKey = cacheKey
            };
        }
        catch (Exception ex)
        {
            throw new AudioCacheException($"Failed to retrieve audio from cache: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the API URL for cached audio.
    /// </summary>
    /// <param name="messageId">The message ID</param>
    /// <returns>Relative API URL</returns>
    public static string GetAudioUrl(Guid messageId)
    {
        return $"{AudioApiBasePath}/{messageId}/audio";
    }
}

/// <summary>
/// Result of retrieving cached audio
/// </summary>
internal class AudioCacheResult
{
    public required Stream Stream { get; set; }
    public required string ContentType { get; set; }
    public required string CacheKey { get; set; }
}

/// <summary>
/// Exception for audio cache operations
/// </summary>
public class AudioCacheException : Exception
{
    public AudioCacheException(string message) : base(message) { }
    public AudioCacheException(string message, Exception innerException) : base(message, innerException) { }
}
