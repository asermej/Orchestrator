using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Manages external API communication for ElevenLabs TTS integration.
/// Handles HTTP requests, streaming responses, and configuration-based guardrails.
/// </summary>
internal sealed class ElevenLabsManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private HttpClient? _httpClient;
    private ElevenLabsConfig? _config;

    private ElevenLabsConfig Config
    {
        get
        {
            if (_config == null)
            {
                var configProvider = _serviceLocator.CreateConfigurationProvider();
                _config = new ElevenLabsConfig
                {
                    Enabled = GetBoolSetting(configProvider, "Enabled", false),
                    UseFakeElevenLabs = GetBoolFromKey(configProvider, "Voice:UseFakeElevenLabs", false),
                    ApiKey = GetStringSetting(configProvider, "ApiKey", ""),
                    BaseUrl = GetStringSetting(configProvider, "BaseUrl", "https://api.elevenlabs.io"),
                    DefaultVoiceId = GetStringSetting(configProvider, "DefaultVoiceId", "21m00Tcm4TlvDq8ikWAM"),
                    ModelId = GetStringSetting(configProvider, "ModelId", "eleven_monolingual_v1"),
                    MaxCharsPerRequest = GetIntSetting(configProvider, "MaxCharsPerRequest", 500),
                    MaxRequestsPerMessage = GetIntSetting(configProvider, "MaxRequestsPerMessage", 6)
                };
            }
            return _config;
        }
    }

    private HttpClient HttpClient
    {
        get
        {
            if (_httpClient == null)
            {
                var config = _serviceLocator.CreateConfigurationProvider();
                var baseUrl = Config.BaseUrl;
                _httpClient = _serviceLocator.CreateHttpClient(baseUrl, 120); // 2 minute timeout for TTS

                // Set authentication header - ElevenLabs uses xi-api-key header
                _httpClient.DefaultRequestHeaders.Add("xi-api-key", Config.ApiKey);
            }
            return _httpClient;
        }
    }

    public ElevenLabsManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// Gets the current ElevenLabs configuration
    /// </summary>
    public ElevenLabsConfig GetConfig() => Config;

    /// <summary>
    /// Checks if ElevenLabs TTS is enabled
    /// </summary>
    public bool IsEnabled() => Config.Enabled;

    /// <summary>
    /// Generates speech audio from text using ElevenLabs streaming API.
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
        if (Config.UseFakeElevenLabs)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            yield break;
        }

        if (!Config.Enabled)
        {
            throw new ElevenLabsDisabledException();
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        // Apply character limit per request
        if (text.Length > Config.MaxCharsPerRequest)
        {
            text = text.Substring(0, Config.MaxCharsPerRequest);
        }

        // Get the streaming response (handles HTTP errors with try-catch)
        var (response, stream) = await GetStreamingResponseAsync(text, voiceId, stability, similarityBoost, cancellationToken).ConfigureAwait(false);
        
        // Stream audio chunks - yield cannot be in try-catch with catch clause
        // Using try-finally is allowed for cleanup
        try
        {
            var buffer = new byte[8192]; // 8KB buffer
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
            {
                var chunk = new byte[bytesRead];
                Array.Copy(buffer, chunk, bytesRead);
                yield return chunk;
            }
        }
        finally
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            response.Dispose();
        }
    }

    /// <summary>
    /// Makes the HTTP request to ElevenLabs and returns the response stream.
    /// Handles all HTTP-level errors with proper exception wrapping.
    /// </summary>
    private async Task<(HttpResponseMessage response, Stream stream)> GetStreamingResponseAsync(
        string text,
        string? voiceId,
        decimal stability,
        decimal similarityBoost,
        CancellationToken cancellationToken)
    {
        var effectiveVoiceId = voiceId ?? Config.DefaultVoiceId;
        
        var request = new ElevenLabsTtsRequest
        {
            Text = text,
            ModelId = Config.ModelId,
            VoiceSettings = new ElevenLabsVoiceSettings
            {
                Stability = stability,
                SimilarityBoost = similarityBoost
            }
        };

        var jsonContent = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        HttpResponseMessage? response = null;
        try
        {
            var url = $"/v1/text-to-speech/{effectiveVoiceId}/stream";
            response = await HttpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                response.Dispose();
                throw new ElevenLabsApiException($"ElevenLabs API returned error: {response.StatusCode} - {errorContent}");
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return (response, stream);
        }
        catch (HttpRequestException ex)
        {
            response?.Dispose();
            throw new ElevenLabsConnectionException($"Failed to connect to ElevenLabs API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            response?.Dispose();
            throw new ElevenLabsConnectionException($"ElevenLabs API request timed out: {ex.Message}", ex);
        }
        catch (ElevenLabsApiException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            response?.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            response?.Dispose();
            throw new ElevenLabsApiException($"Unexpected error calling ElevenLabs API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Lists available voices (prebuilt and optionally user clones). When UseFakeElevenLabs is true, returns deterministic fake list.
    /// </summary>
    public async Task<IReadOnlyList<ElevenLabsVoiceItem>> ListVoicesAsync(CancellationToken cancellationToken = default)
    {
        if (Config.UseFakeElevenLabs)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return new List<ElevenLabsVoiceItem>
            {
                new ElevenLabsVoiceItem { Id = "fake-voice-1", Name = "Fake Voice One", VoiceType = "prebuilt", PreviewText = "Hey — I'm your Surrova persona voice." },
                new ElevenLabsVoiceItem { Id = "fake-voice-2", Name = "Fake Voice Two", VoiceType = "prebuilt", PreviewText = "Hey — I'm your Surrova persona voice." }
            };
        }

        if (!Config.Enabled)
        {
            throw new ElevenLabsDisabledException();
        }

        try
        {
            var response = await HttpClient.GetAsync("/v1/voices", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new ElevenLabsApiException($"ElevenLabs API returned error: {response.StatusCode} - {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var voices = new List<ElevenLabsVoiceItem>();
            if (root.TryGetProperty("voices", out var voicesArray))
            {
                foreach (var v in voicesArray.EnumerateArray())
                {
                    var id = v.TryGetProperty("voice_id", out var idEl) ? idEl.GetString() ?? "" : "";
                    var name = v.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
                    var category = v.TryGetProperty("category", out var catEl) ? catEl.GetString() : null;
                    voices.Add(new ElevenLabsVoiceItem { Id = id, Name = name, VoiceType = "prebuilt", Category = category });
                }
            }
            return voices;
        }
        catch (HttpRequestException ex)
        {
            throw new ElevenLabsConnectionException($"Failed to connect to ElevenLabs API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ElevenLabsConnectionException($"ElevenLabs API request timed out: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a voice from a sample (Instant Voice Cloning). When UseFakeElevenLabs is true, returns deterministic fake voice ID.
    /// </summary>
    /// <param name="voiceName">Display name for the voice</param>
    /// <param name="sampleAudioBytes">Raw audio bytes (e.g. MP3/WAV); caller downloads from blob if needed</param>
    /// <param name="fileName">Suggested filename for the multipart upload (e.g. "sample.mp3")</param>
    public async Task<VoiceCloneResult> CreateVoiceFromSampleAsync(string voiceName, byte[] sampleAudioBytes, string fileName = "sample.mp3", CancellationToken cancellationToken = default)
    {
        if (Config.UseFakeElevenLabs)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return new VoiceCloneResult { VoiceId = "fake-cloned-voice-id", VoiceName = voiceName };
        }

        if (!Config.Enabled)
        {
            throw new ElevenLabsDisabledException();
        }

        try
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(voiceName), "name");
            content.Add(new ByteArrayContent(sampleAudioBytes), "files", fileName);

            var response = await HttpClient.PostAsync("/v1/voices/add", content, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new ElevenLabsApiException($"ElevenLabs API returned error: {response.StatusCode} - {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var voiceId = root.TryGetProperty("voice_id", out var idEl) ? idEl.GetString() ?? "" : "";
            return new VoiceCloneResult { VoiceId = voiceId, VoiceName = voiceName };
        }
        catch (HttpRequestException ex)
        {
            throw new ElevenLabsConnectionException($"Failed to connect to ElevenLabs API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ElevenLabsConnectionException($"ElevenLabs API request timed out: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generates speech audio from text (non-streaming, returns complete audio).
    /// Use this for caching scenarios where you need the complete audio.
    /// </summary>
    public async Task<byte[]> GenerateSpeechAsync(
        string text,
        string? voiceId = null,
        decimal stability = 0.5m,
        decimal similarityBoost = 0.75m,
        CancellationToken cancellationToken = default)
    {
        if (Config.UseFakeElevenLabs)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return Array.Empty<byte>();
        }

        var chunks = new List<byte[]>();
        
        await foreach (var chunk in StreamSpeechAsync(text, voiceId, stability, similarityBoost, cancellationToken).ConfigureAwait(false))
        {
            chunks.Add(chunk);
        }

        // Combine all chunks into single byte array
        var totalLength = 0;
        foreach (var chunk in chunks)
        {
            totalLength += chunk.Length;
        }

        var result = new byte[totalLength];
        var offset = 0;
        foreach (var chunk in chunks)
        {
            Array.Copy(chunk, 0, result, offset, chunk.Length);
            offset += chunk.Length;
        }

        return result;
    }

    private string GetStringSetting(ConfigurationProviderBase config, string key, string defaultValue)
    {
        var value = config.GetGatewaySetting("ElevenLabs", key);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private int GetIntSetting(ConfigurationProviderBase config, string key, int defaultValue)
    {
        var value = config.GetGatewaySetting("ElevenLabs", key);
        if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out var result))
        {
            return defaultValue;
        }
        return result;
    }

    private bool GetBoolSetting(ConfigurationProviderBase config, string key, bool defaultValue)
    {
        var value = config.GetGatewaySetting("ElevenLabs", key);
        if (string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var result))
        {
            return defaultValue;
        }
        return result;
    }

    private bool GetBoolFromKey(ConfigurationProviderBase config, string fullKey, bool defaultValue)
    {
        var value = config.GetConfigurationValue(fullKey);
        if (string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var result))
        {
            return defaultValue;
        }
        return result;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
