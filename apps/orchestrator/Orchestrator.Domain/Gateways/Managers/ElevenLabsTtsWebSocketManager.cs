using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Manages a ClientWebSocket connection to the ElevenLabs TTS stream-input API.
/// Sends text and receives audio chunks via WebSocket for low-latency synthesis.
/// Each instance handles one synthesis request; create a new instance per sentence/turn.
/// Supports both ulaw_8000 (phone/Telnyx) and mp3_44100_128 (web browser) output formats.
/// </summary>
internal sealed class ElevenLabsTtsWebSocketManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private ClientWebSocket? _preConnectedWs;
    private bool _disposed;

    private const string TtsBaseUrl = "wss://api.elevenlabs.io/v1/text-to-speech";
    public const string FormatUlaw8000 = "ulaw_8000";
    public const string FormatMp3 = "mp3_44100_128";

    public ElevenLabsTtsWebSocketManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// LATENCY OPTIMIZATION: Opens the WebSocket connection and sends the INIT message
    /// ahead of time, so the next SynthesizeAsync call skips the connection overhead.
    /// </summary>
    public async Task PreConnectAsync(
        string? voiceId = null,
        string outputFormat = FormatMp3,
        CancellationToken cancellationToken = default)
    {
        var config = LoadConfig();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new ElevenLabsConnectionException("ElevenLabs API key is not configured for TTS WebSocket");

        var effectiveVoiceId = voiceId ?? config.DefaultVoiceId;
        var modelId = config.TtsWebSocketModelId;
        var url = $"{TtsBaseUrl}/{effectiveVoiceId}/stream-input?output_format={outputFormat}&model_id={modelId}";

        Console.WriteLine($"[TTS-WS] Pre-connecting to ElevenLabs TTS: voice={effectiveVoiceId}, model={modelId}, format={outputFormat}");

        var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("xi-api-key", config.ApiKey);

        try
        {
            await ws.ConnectAsync(new Uri(url), cancellationToken).ConfigureAwait(false);

            var initMsg = new ElevenLabsTtsWsInitMessage
            {
                ApiKey = config.ApiKey,
                VoiceSettings = new ElevenLabsTtsWsVoiceSettings
                {
                    Stability = 0.5,
                    SimilarityBoost = 0.75,
                    Style = 0,
                    UseSpeakerBoost = true,
                    Speed = 1.0
                }
            };
            await SendJsonAsync(ws, initMsg, cancellationToken).ConfigureAwait(false);

            Console.WriteLine("[TTS-WS] ✓ Pre-connected and initialized");
            _preConnectedWs = ws;
        }
        catch
        {
            ws.Dispose();
            throw;
        }
    }

    /// <summary>
    /// LATENCY-CRITICAL: Synthesizes text to raw audio bytes via the ElevenLabs WebSocket TTS API.
    /// Yields decoded byte[] chunks as they arrive — suitable for writing directly to an HTTP response.
    /// </summary>
    /// <param name="text">Text to synthesize</param>
    /// <param name="voiceId">ElevenLabs voice ID</param>
    /// <param name="outputFormat">Audio format (e.g. "mp3_44100_128" for web, "ulaw_8000" for phone)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of raw audio byte chunks</returns>
    public async IAsyncEnumerable<byte[]> SynthesizeBytesAsync(
        string text,
        string? voiceId = null,
        string outputFormat = FormatMp3,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var base64Chunk in SynthesizeAsync(text, voiceId, outputFormat, cancellationToken).ConfigureAwait(false))
        {
            yield return Convert.FromBase64String(base64Chunk);
        }
    }

    /// <summary>
    /// Synthesizes text to audio via the ElevenLabs WebSocket TTS API.
    /// Yields base64-encoded audio chunks as they arrive.
    /// </summary>
    /// <param name="text">Text to synthesize</param>
    /// <param name="voiceId">ElevenLabs voice ID</param>
    /// <param name="outputFormat">Audio format (default "ulaw_8000" for backward compat with phone path)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of base64-encoded audio chunks</returns>
    public async IAsyncEnumerable<string> SynthesizeAsync(
        string text,
        string? voiceId = null,
        string outputFormat = FormatUlaw8000,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var config = LoadConfig();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new ElevenLabsConnectionException("ElevenLabs API key is not configured for TTS WebSocket");
        }

        // Reuse pre-connected socket if available (from PreConnectAsync), otherwise connect fresh
        var ws = _preConnectedWs;
        _preConnectedWs = null;

        if (ws == null)
        {
            var effectiveVoiceId = voiceId ?? config.DefaultVoiceId;
            var modelId = config.TtsWebSocketModelId;
            var url = $"{TtsBaseUrl}/{effectiveVoiceId}/stream-input?output_format={outputFormat}&model_id={modelId}";

            Console.WriteLine($"[TTS-WS] Connecting to ElevenLabs TTS: voice={effectiveVoiceId}, model={modelId}, format={outputFormat}");

            ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("xi-api-key", config.ApiKey);

            try
            {
                await ws.ConnectAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
                Console.WriteLine("[TTS-WS] ✓ Connected");
            }
            catch (WebSocketException ex)
            {
                ws.Dispose();
                Console.WriteLine($"[TTS-WS] ✗ Connection failed: {ex.Message}");
                throw new ElevenLabsConnectionException($"Failed to connect to ElevenLabs TTS WebSocket: {ex.Message}", ex);
            }

            var initMsg = new ElevenLabsTtsWsInitMessage
            {
                ApiKey = config.ApiKey,
                VoiceSettings = new ElevenLabsTtsWsVoiceSettings
                {
                    Stability = 0.5,
                    SimilarityBoost = 0.75,
                    Style = 0,
                    UseSpeakerBoost = true,
                    Speed = 1.0
                }
            };
            var initJson = JsonSerializer.Serialize(initMsg);
            Console.WriteLine($"[TTS-WS] >>> INIT: {initJson}");
            await SendJsonAsync(ws, initMsg, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            Console.WriteLine("[TTS-WS] Using pre-warmed WebSocket connection");
        }

        // 2. Send the text
        var textMsg = new ElevenLabsTtsWsTextMessage
        {
            Text = text + " ",
            TryTriggerGeneration = true,
            Flush = true
        };
        var textJson = JsonSerializer.Serialize(textMsg);
        Console.WriteLine($"[TTS-WS] >>> TEXT: {textJson}");
        await SendJsonAsync(ws, textMsg, cancellationToken).ConfigureAwait(false);

        // 3. Send close/flush signal (empty text)
        var closeMsg = new ElevenLabsTtsWsCloseMessage();
        Console.WriteLine("[TTS-WS] >>> CLOSE signal (empty text)");
        await SendJsonAsync(ws, closeMsg, cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"[TTS-WS] Waiting for audio response (ws state: {ws.State})...");

        // 4. Receive audio chunks
        var buffer = new byte[8192];
        int msgIndex = 0;

        while (!cancellationToken.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            var message = await ReceiveFullMessageAsync(ws, buffer, cancellationToken).ConfigureAwait(false);
            if (message == null)
            {
                Console.WriteLine($"[TTS-WS] <<< Received null (connection closed), ws state: {ws.State}");
                break;
            }

            msgIndex++;

            // Log the raw response for the first few messages and any non-audio messages
            var response = JsonSerializer.Deserialize<ElevenLabsTtsWsAudioResponse>(message);

            if (response == null)
            {
                Console.WriteLine($"[TTS-WS] <<< MSG #{msgIndex}: Failed to deserialize: {(message.Length > 300 ? message[..300] + "..." : message)}");
                continue;
            }

            bool hasAudio = !string.IsNullOrEmpty(response.Audio);

            if (msgIndex <= 3 || response.IsFinal || !hasAudio)
            {
                Console.WriteLine($"[TTS-WS] <<< MSG #{msgIndex}: isFinal={response.IsFinal}, hasAudio={hasAudio}, audioLen={response.Audio?.Length ?? 0}");
            }

            if (response.IsFinal)
            {
                Console.WriteLine($"[TTS-WS] Received isFinal after {msgIndex} messages");
                break;
            }

            if (hasAudio)
            {
                yield return response.Audio!;
            }
        }

        Console.WriteLine($"[TTS-WS] Done receiving, total messages: {msgIndex}, ws state: {ws.State}");

        if (ws.State == WebSocketState.Open)
        {
            try
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None).ConfigureAwait(false);
            }
            catch (WebSocketException)
            {
                // Already closed
            }
        }

        ws.Dispose();
    }

    private static async Task SendJsonAsync<T>(ClientWebSocket ws, T message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string?> ReceiveFullMessageAsync(ClientWebSocket ws, byte[] buffer, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Close)
                return null;

            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        }
        while (!result.EndOfMessage);

        return sb.ToString();
    }

    private ElevenLabsTtsWsConfig LoadConfig()
    {
        var configProvider = _serviceLocator.CreateConfigurationProvider();
        var defaultVoiceId = configProvider.GetGatewaySetting("ElevenLabs", "DefaultVoiceId") ?? "";

        Console.WriteLine($"[TTS-WS] Config loaded: voiceId={defaultVoiceId}, model={configProvider.GetGatewaySetting("ElevenLabs", "TtsWebSocketModelId") ?? "eleven_turbo_v2_5"}");

        return new ElevenLabsTtsWsConfig
        {
            ApiKey = configProvider.GetGatewaySetting("ElevenLabs", "ApiKey") ?? "",
            DefaultVoiceId = defaultVoiceId,
            TtsWebSocketModelId = configProvider.GetGatewaySetting("ElevenLabs", "TtsWebSocketModelId")
                                  ?? "eleven_turbo_v2_5"
        };
    }

    public void Dispose()
    {
        _preConnectedWs?.Dispose();
        _preConnectedWs = null;
        _disposed = true;
    }
}

/// <summary>
/// Configuration for ElevenLabs WebSocket-based TTS (phone calls and web interview)
/// </summary>
internal sealed class ElevenLabsTtsWsConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultVoiceId { get; set; } = "21m00Tcm4TlvDq8ikWAM";
    public string TtsWebSocketModelId { get; set; } = "eleven_turbo_v2_5";
}
