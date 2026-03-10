using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Orchestrates one active phone call: bridges Telnyx audio with ElevenLabs STT,
/// OpenAI streaming chat completion, and ElevenLabs TTS to produce a conversational AI agent.
/// </summary>
public sealed class PhoneCallSession : IAsyncDisposable
{
    private readonly GatewayFacade _gatewayFacade;
    private readonly string _systemPrompt;
    private readonly string? _voiceId;
    private readonly List<ConversationTurn> _chatHistory = new();
    private readonly SemaphoreSlim _responseLock = new(1, 1);

    private ElevenLabsSttManager? _sttManager;
    private CancellationTokenSource? _sessionCts;
    private bool _disposed;
    private int _turnCount;

    /// <summary>
    /// Raised when TTS audio is ready to be forwarded to the caller via Telnyx.
    /// The string is a base64-encoded ulaw audio chunk.
    /// </summary>
    public Func<string, Task>? OnTtsAudioReady;


    internal PhoneCallSession(GatewayFacade gatewayFacade, string systemPrompt, string? voiceId = null)
    {
        _gatewayFacade = gatewayFacade ?? throw new ArgumentNullException(nameof(gatewayFacade));
        _systemPrompt = systemPrompt;
        _voiceId = voiceId;
        Console.WriteLine($"[PHONE][SESSION] Created with voice={voiceId ?? "(default)"}, prompt length={systemPrompt.Length} chars");
    }

    /// <summary>
    /// Starts the session: opens the ElevenLabs STT WebSocket and begins listening for transcripts.
    /// When a committed transcript arrives, it is fed through the OpenAI -> TTS pipeline.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[PHONE][SESSION] Starting — connecting to ElevenLabs STT...");
        _sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _sttManager = _gatewayFacade.CreateElevenLabsSttManager();
        await _sttManager.ConnectAndListenAsync(OnTranscriptReceived, _sessionCts.Token).ConfigureAwait(false);

        Console.WriteLine("[PHONE][SESSION] ✓ Started — STT connected, waiting for caller speech");
    }

    /// <summary>
    /// Forwards a base64-encoded ulaw audio chunk from Telnyx to the ElevenLabs STT WebSocket.
    /// </summary>
    public async Task ProcessAudioFromCallerAsync(string base64Audio)
    {
        if (_sttManager == null || _sessionCts == null)
            return;

        await _sttManager.SendAudioChunkAsync(base64Audio, _sessionCts.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Called when ElevenLabs STT emits a committed transcript.
    /// Feeds the transcript through OpenAI streaming and TTS, then emits audio via OnTtsAudioReady.
    /// </summary>
    private void OnTranscriptReceived(string transcript)
    {
        var turn = Interlocked.Increment(ref _turnCount);
        Console.WriteLine($"[PHONE][TRANSCRIPT] 📝 Turn #{turn}: \"{transcript}\"");

        _ = Task.Run(async () =>
        {
            await _responseLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await GenerateAndSpeakResponseAsync(transcript, turn).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PHONE][SESSION] ✗ Error in turn #{turn}: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                _responseLock.Release();
            }
        });
    }

    /// <summary>
    /// Streams an AI response for the given user transcript and sends each sentence to TTS.
    /// </summary>
    private async Task GenerateAndSpeakResponseAsync(string userMessage, int turn)
    {
        if (_sessionCts == null)
            return;

        var ct = _sessionCts.Token;
        var sw = Stopwatch.StartNew();

        _chatHistory.Add(new ConversationTurn { Role = "user", Content = userMessage });
        Console.WriteLine($"[PHONE][OPENAI] Turn #{turn}: Streaming chat completion ({_chatHistory.Count} messages in history)...");

        var fullResponse = new StringBuilder();
        var sentenceBuffer = new StringBuilder();
        int tokenCount = 0;
        int sentenceCount = 0;
        bool firstToken = true;

        await foreach (var token in _gatewayFacade.StreamAnthropicCompletionAsync(_systemPrompt, _chatHistory, ct).ConfigureAwait(false))
        {
            if (firstToken)
            {
                Console.WriteLine($"[PHONE][OPENAI] Turn #{turn}: First token arrived in {sw.ElapsedMilliseconds}ms");
                firstToken = false;
            }

            tokenCount++;
            fullResponse.Append(token);
            sentenceBuffer.Append(token);

            var buffered = sentenceBuffer.ToString();
            var sentenceEnd = SentenceBuffer.FindSentenceEnd(buffered);

            if (sentenceEnd >= 0)
            {
                var sentence = buffered.Substring(0, sentenceEnd + 1).Trim();
                sentenceBuffer.Remove(0, sentenceEnd + 1);

                if (!string.IsNullOrWhiteSpace(sentence))
                {
                    sentenceCount++;
                    Console.WriteLine($"[PHONE][TTS] Turn #{turn}, sentence #{sentenceCount}: \"{sentence}\"");
                    await SynthesizeAndEmitAsync(sentence, ct).ConfigureAwait(false);
                }
            }
        }

        // Flush any remaining text in the buffer
        var remaining = sentenceBuffer.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(remaining))
        {
            sentenceCount++;
            Console.WriteLine($"[PHONE][TTS] Turn #{turn}, sentence #{sentenceCount} (flush): \"{remaining}\"");
            await SynthesizeAndEmitAsync(remaining, ct).ConfigureAwait(false);
        }

        _chatHistory.Add(new ConversationTurn { Role = "assistant", Content = fullResponse.ToString() });

        sw.Stop();
        Console.WriteLine($"[PHONE][OPENAI] Turn #{turn} complete: {tokenCount} tokens, {sentenceCount} sentences, {sw.ElapsedMilliseconds}ms total");
    }

    /// <summary>
    /// Sends a sentence to ElevenLabs TTS and emits each audio chunk via the OnTtsAudioReady event.
    /// </summary>
    private async Task SynthesizeAndEmitAsync(string sentence, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[PHONE][TTS→EMIT] Starting synthesis for: \"{(sentence.Length > 80 ? sentence[..80] + "..." : sentence)}\"");

        using var ttsManager = _gatewayFacade.CreateElevenLabsTtsWebSocketManager();
        int chunkCount = 0;
        int emittedCount = 0;

        await foreach (var audioBase64 in ttsManager.SynthesizeAsync(sentence, _voiceId, ElevenLabsTtsWebSocketManager.FormatUlaw8000, cancellationToken).ConfigureAwait(false))
        {
            chunkCount++;
            if (chunkCount == 1)
            {
                Console.WriteLine($"[PHONE][TTS→EMIT] First audio chunk received ({audioBase64.Length} base64 chars)");
            }

            if (OnTtsAudioReady != null)
            {
                await OnTtsAudioReady(audioBase64).ConfigureAwait(false);
                emittedCount++;
            }
            else
            {
                Console.WriteLine("[PHONE][TTS→EMIT] ✗ OnTtsAudioReady is null — audio chunk dropped!");
            }
        }

        Console.WriteLine($"[PHONE][TTS→EMIT] Done: {chunkCount} chunks from TTS, {emittedCount} emitted to Telnyx");
    }

    public async Task StopAsync()
    {
        Console.WriteLine("[PHONE][SESSION] Stopping...");
        _sessionCts?.Cancel();

        if (_sttManager != null)
        {
            await _sttManager.CloseAsync().ConfigureAwait(false);
        }

        Console.WriteLine($"[PHONE][SESSION] ■ Stopped ({_turnCount} conversation turns processed)");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _sessionCts?.Cancel();

        if (_sttManager != null)
        {
            try { await _sttManager.CloseAsync().ConfigureAwait(false); }
            catch { /* cleanup */ }
            _sttManager.Dispose();
        }

        _sessionCts?.Dispose();
        _responseLock.Dispose();
        Console.WriteLine("[PHONE][SESSION] Disposed");
    }
}
