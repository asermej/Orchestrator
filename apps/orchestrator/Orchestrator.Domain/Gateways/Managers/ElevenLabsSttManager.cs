using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Manages a ClientWebSocket connection to the ElevenLabs realtime speech-to-text API.
/// Receives ulaw_8000 audio and emits committed transcripts via callback.
/// </summary>
internal sealed class ElevenLabsSttManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _listenCts;
    private Task? _listenTask;
    private bool _disposed;

    private const string SttBaseUrl = "wss://api.elevenlabs.io/v1/speech-to-text/realtime";

    public ElevenLabsSttManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// Opens a WebSocket connection to ElevenLabs realtime STT and begins listening
    /// for transcript messages in the background.
    /// </summary>
    /// <param name="onTranscript">Callback invoked with the final committed transcript text.</param>
    /// <param name="cancellationToken">Cancellation token for the connect operation.</param>
    public async Task ConnectAndListenAsync(Action<string> onTranscript, CancellationToken cancellationToken = default)
    {
        var config = LoadConfig();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            Console.WriteLine("[PHONE][STT] ✗ ElevenLabs API key is not configured");
            throw new ElevenLabsConnectionException("ElevenLabs API key is not configured for STT");
        }

        var url = BuildSttUrl();
        Console.WriteLine($"[PHONE][STT] Connecting to ElevenLabs STT: {url}");

        _webSocket = new ClientWebSocket();
        _webSocket.Options.SetRequestHeader("xi-api-key", config.ApiKey);

        try
        {
            await _webSocket.ConnectAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"[PHONE][STT] ✓ Connected (state: {_webSocket.State})");
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"[PHONE][STT] ✗ Connection failed: {ex.Message}");
            throw new ElevenLabsConnectionException($"Failed to connect to ElevenLabs STT: {ex.Message}", ex);
        }

        _listenCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listenTask = Task.Run(() => ListenLoopAsync(onTranscript, _listenCts.Token), _listenCts.Token);
        Console.WriteLine("[PHONE][STT] Listening for transcripts in background...");
    }

    /// <summary>
    /// Sends a base64-encoded ulaw audio chunk to the STT WebSocket.
    /// </summary>
    public async Task SendAudioChunkAsync(string base64Audio, CancellationToken cancellationToken = default)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            return;

        var chunk = new ElevenLabsSttAudioChunk
        {
            AudioBase64 = base64Audio,
            SampleRate = 8000,
            Commit = false
        };

        var json = JsonSerializer.Serialize(chunk);
        var bytes = Encoding.UTF8.GetBytes(json);

        try
        {
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken).ConfigureAwait(false);
        }
        catch (WebSocketException)
        {
            // Connection lost; the listen loop will detect and clean up
        }
    }

    /// <summary>
    /// Background loop that reads messages from the STT WebSocket and dispatches transcripts.
    /// </summary>
    private async Task ListenLoopAsync(Action<string> onTranscript, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   _webSocket != null &&
                   _webSocket.State == WebSocketState.Open)
            {
                var result = await ReceiveFullMessageAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (result == null)
                    break;

                var response = JsonSerializer.Deserialize<ElevenLabsSttResponse>(result);
                if (response == null)
                    continue;

                switch (response.MessageType)
                {
                    case "committed_transcript":
                        var text = response.Text?.Trim();
                        if (!string.IsNullOrEmpty(text))
                        {
                            Console.WriteLine($"[PHONE][STT] 📝 Committed transcript: \"{text}\"");
                            onTranscript(text);
                        }
                        break;

                    case "partial_transcript":
                        var partial = response.Text?.Trim();
                        if (!string.IsNullOrEmpty(partial))
                        {
                            Console.WriteLine($"[PHONE][STT] (partial) \"{partial}\"");
                        }
                        break;

                    case "session_started":
                        Console.WriteLine($"[PHONE][STT] ✓ Session started: {response.SessionId}");
                        break;

                    case "auth_error":
                    case "quota_exceeded":
                    case "rate_limited":
                    case "input_error":
                        Console.WriteLine($"[PHONE][STT] ✗ Error: {response.MessageType} — {response.Error ?? response.Message}");
                        break;

                    default:
                        Console.WriteLine($"[PHONE][STT] Unknown message type: {response.MessageType}");
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[PHONE][STT] Listen loop cancelled (shutdown)");
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"[PHONE][STT] Listen loop WebSocket error: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads a complete WebSocket text message, handling fragmented frames.
    /// </summary>
    private async Task<string?> ReceiveFullMessageAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        if (_webSocket == null)
            return null;

        var sb = new StringBuilder();

        WebSocketReceiveResult result;
        do
        {
            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Close)
                return null;

            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        }
        while (!result.EndOfMessage);

        return sb.ToString();
    }

    private string BuildSttUrl()
    {
        return $"{SttBaseUrl}?audio_format=ulaw_8000&commit_strategy=vad&language_code=en&vad_silence_threshold_secs=1.5&include_timestamps=false";
    }

    private ElevenLabsConfig LoadConfig()
    {
        var configProvider = _serviceLocator.CreateConfigurationProvider();
        return new ElevenLabsConfig
        {
            ApiKey = configProvider.GetGatewaySetting("ElevenLabs", "ApiKey") ?? "",
            BaseUrl = configProvider.GetGatewaySetting("ElevenLabs", "BaseUrl") ?? "https://api.elevenlabs.io"
        };
    }

    public async Task CloseAsync()
    {
        _listenCts?.Cancel();

        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None).ConfigureAwait(false);
            }
            catch (WebSocketException)
            {
                // Already closed
            }
        }

        if (_listenTask != null)
        {
            try { await _listenTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _listenCts?.Cancel();
        _listenCts?.Dispose();
        _webSocket?.Dispose();
    }
}
