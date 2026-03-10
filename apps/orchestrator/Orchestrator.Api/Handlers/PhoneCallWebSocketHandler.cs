using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Orchestrator.Domain;

namespace Orchestrator.Api.Handlers;

/// <summary>
/// Handles the Telnyx bidirectional media stream WebSocket at /api/v1/phonecall/media-stream.
/// Bridges the Telnyx audio with a PhoneCallSession that orchestrates STT, OpenAI, and TTS.
/// </summary>
public static class PhoneCallWebSocketHandler
{
    private const string DefaultSystemPrompt =
        "You are a helpful phone assistant. " +
        "Respond naturally and conversationally. " +
        "Keep responses concise — one or two sentences at a time. " +
        "Do not use markdown, bullet points, or any text formatting.";

    public static async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            Console.WriteLine("[PHONE][WS] Rejected non-WebSocket request to media-stream endpoint");
            context.Response.StatusCode = 400;
            return;
        }

        var callStopwatch = Stopwatch.StartNew();
        var callId = Guid.NewGuid().ToString("N")[..8];

        Console.WriteLine("────────────────────────────────────────────────────────────");
        Console.WriteLine($"[PHONE][WS][{callId}] 🔌 Accepting Telnyx media stream WebSocket...");

        using var telnyxWs = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine($"[PHONE][WS][{callId}] ✓ WebSocket connected (state: {telnyxWs.State})");

        using var domainFacade = context.RequestServices.GetRequiredService<DomainFacade>();

        await using var session = domainFacade.CreatePhoneCallSession(DefaultSystemPrompt);

        var sendLock = new SemaphoreSlim(1, 1);
        long audioChunksReceived = 0;
        long audioChunksSentToTelnyx = 0;

        // Subscribe to TTS audio and forward back to Telnyx
        session.OnTtsAudioReady = async (audioBase64) =>
        {
            if (telnyxWs.State != WebSocketState.Open)
                return;

            var mediaMessage = JsonSerializer.Serialize(new
            {
                @event = "media",
                media = new { payload = audioBase64 }
            });

            var bytes = Encoding.UTF8.GetBytes(mediaMessage);

            await sendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (telnyxWs.State == WebSocketState.Open)
                {
                    await telnyxWs.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        CancellationToken.None).ConfigureAwait(false);

                    var count = Interlocked.Increment(ref audioChunksSentToTelnyx);
                    if (count % 50 == 1)
                    {
                        Console.WriteLine($"[PHONE][TTS→TELNYX][{callId}] Sent {count} audio chunks back to caller");
                    }
                }
            }
            catch (WebSocketException)
            {
                Console.WriteLine($"[PHONE][TTS→TELNYX][{callId}] ✗ WebSocket closed while sending TTS audio");
            }
            finally
            {
                sendLock.Release();
            }
        };

        Console.WriteLine($"[PHONE][WS][{callId}] Starting STT pipeline...");
        await session.StartAsync(context.RequestAborted).ConfigureAwait(false);
        Console.WriteLine($"[PHONE][WS][{callId}] ✓ STT pipeline started — listening for caller audio");

        var buffer = new byte[4096];

        try
        {
            while (telnyxWs.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
            {
                var message = await ReceiveFullMessageAsync(telnyxWs, buffer, context.RequestAborted).ConfigureAwait(false);
                if (message == null)
                {
                    Console.WriteLine($"[PHONE][WS][{callId}] Received null message (connection closing)");
                    break;
                }

                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                var eventType = root.TryGetProperty("event", out var eventEl) ? eventEl.GetString() : null;

                switch (eventType)
                {
                    case "start":
                        Console.WriteLine($"[PHONE][WS][{callId}] ▶ Telnyx media stream STARTED");
                        if (root.TryGetProperty("stream_id", out var streamIdEl))
                        {
                            Console.WriteLine($"[PHONE][WS][{callId}]   Stream ID: {streamIdEl.GetString()}");
                        }
                        Console.WriteLine($"[PHONE][WS][{callId}]   Full start payload: {message}");
                        break;

                    case "media":
                        if (root.TryGetProperty("media", out var mediaEl) &&
                            mediaEl.TryGetProperty("payload", out var payloadEl))
                        {
                            var payload = payloadEl.GetString();
                            if (!string.IsNullOrEmpty(payload))
                            {
                                var count = Interlocked.Increment(ref audioChunksReceived);
                                if (count == 1)
                                {
                                    Console.WriteLine($"[PHONE][AUDIO][{callId}] 🎤 First audio chunk received from caller ({payload.Length} base64 chars)");
                                }
                                else if (count % 100 == 0)
                                {
                                    Console.WriteLine($"[PHONE][AUDIO][{callId}] Received {count} audio chunks from caller so far");
                                }

                                await session.ProcessAudioFromCallerAsync(payload).ConfigureAwait(false);
                            }
                        }
                        break;

                    case "stop":
                        Console.WriteLine($"[PHONE][WS][{callId}] ■ Telnyx media stream STOPPED");
                        goto exitLoop;

                    default:
                        Console.WriteLine($"[PHONE][WS][{callId}] Unhandled Telnyx event: {eventType} — {message}");
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[PHONE][WS][{callId}] Call cancelled (request aborted)");
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"[PHONE][WS][{callId}] WebSocket error: {ex.Message}");
        }

        exitLoop:

        await session.StopAsync().ConfigureAwait(false);

        if (telnyxWs.State == WebSocketState.Open)
        {
            try
            {
                await telnyxWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None).ConfigureAwait(false);
            }
            catch (WebSocketException)
            {
                // Already closed
            }
        }

        sendLock.Dispose();
        callStopwatch.Stop();

        Console.WriteLine($"[PHONE][WS][{callId}] ═══ CALL ENDED ═══");
        Console.WriteLine($"[PHONE][WS][{callId}]   Duration:             {callStopwatch.Elapsed:mm\\:ss\\.fff}");
        Console.WriteLine($"[PHONE][WS][{callId}]   Audio chunks received: {audioChunksReceived}");
        Console.WriteLine($"[PHONE][WS][{callId}]   Audio chunks sent:     {audioChunksSentToTelnyx}");
        Console.WriteLine("────────────────────────────────────────────────────────────");
    }

    private static async Task<string?> ReceiveFullMessageAsync(WebSocket ws, byte[] buffer, CancellationToken cancellationToken)
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
}
