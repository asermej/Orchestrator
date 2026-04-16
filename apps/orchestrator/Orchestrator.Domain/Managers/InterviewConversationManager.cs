using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// LATENCY-CRITICAL: Handles the streaming conversation turn pipeline for web interviews.
/// Generates an AI response via streaming OpenAI, buffers to sentences, pipes through
/// ElevenLabs WebSocket TTS, and yields MP3 audio chunks for direct HTTP streaming.
/// Modeled after PhoneCallSession but optimized for the web interview path.
/// </summary>
internal sealed class InterviewConversationManager : IDisposable
{
    /// <summary>Populated for <c>[INTERVIEW][METRICS_ROW]</c> JSON logs (join with FE via <see cref="RespondToTurnRequest.CorrelationId"/>).</summary>
    private sealed class RespondToTurnPipelineMetrics
    {
        public long? FirstTokenMs { get; set; }
        public long? MarkerDetectedMs { get; set; }
        public long LlmTotalMs { get; set; }
        public bool UsedFallbackParse { get; set; }
        public long TtsFirstAudioMs { get; set; }
        public long TtsTotalMs { get; set; }
        public int TtsSentenceCount { get; set; }
    }

    private readonly ServiceLocatorBase _serviceLocator;
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);

    private static readonly Regex MarkerLineRegex = new(
        @"^\[(?:FOLLOW_UP:(\w+)|TRANSITION|REPEAT|OFF_TOPIC|LANGUAGE_SWITCH:(\w+)|END_INTERVIEW)\]$",
        RegexOptions.Compiled);

    private static readonly Regex MarkerLastRegex = new(
        @"\[(?:FOLLOW_UP:(\w+)|TRANSITION|REPEAT|OFF_TOPIC|LANGUAGE_SWITCH:(\w+)|END_INTERVIEW)\]\s*$",
        RegexOptions.Compiled);

    private static readonly Regex EvalLineRegex = new(
        @"^\[EVAL:action=(\w+),result=(\w+)(?:,score=(\d))?\]$",
        RegexOptions.Compiled);

    /// <summary>Matches a leading [EVAL:...] line so it can be stripped from spoken text (avoids leaking internal eval to user).</summary>
    private static readonly Regex LeadingEvalLineRegex = new(
        @"^\[EVAL:[^\]]+\]\s*[\r\n]*",
        RegexOptions.Compiled);

    /// <summary>Matches any known response marker that may leak into spoken text sent to TTS.</summary>
    private static readonly Regex AnyMarkerRegex = new(
        @"\[(?:FOLLOW_UP:\w+|TRANSITION|REPEAT|OFF_TOPIC|LANGUAGE_SWITCH:\w+|END_INTERVIEW|EVAL:[^\]]+)\]",
        RegexOptions.Compiled);

    public InterviewConversationManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// LATENCY-CRITICAL: Concurrent conversation turn pipeline — streams LLM tokens and
    /// TTS synthesis simultaneously via Channels. Audio chunks flow to the browser as soon
    /// as TTS produces them, without waiting for the LLM to finish generating all tokens.
    /// Sonnet evaluates AND responds in a single pass; no separate QuickEval call.
    /// </summary>
    public async IAsyncEnumerable<byte[]> RespondToTurnAsync(
        InterviewRuntimeContext context,
        RespondToTurnRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var turnSw = Stopwatch.StartNew();

        string systemPromptStatic;
        string? systemPromptInterview;
        if (context.Agent != null)
        {
            var parts = new InterviewRuntimeManager(_serviceLocator).BuildInterviewSystemPromptParts(
                context.Agent, context.Template, context.Role, context.ApplicantName, context.JobTitle);
            systemPromptStatic = parts.StaticPart;
            systemPromptInterview = parts.InterviewPart;
        }
        else
        {
            systemPromptStatic = "";
            systemPromptInterview = null;
        }

        var chatHistory = BuildChatHistory(request);
        var userMessage = BuildMergedPrompt(request);
        chatHistory.Add(new ConversationTurn { Role = "user", Content = userMessage });

        var voiceId = context.Agent?.ElevenlabsVoiceId ?? "21m00Tcm4TlvDq8ikWAM";

        var audioChannel = Channel.CreateUnbounded<byte[]>();

        var pipelineTask = RunLlmPipelineAsync(
            audioChannel.Writer,
            systemPromptStatic, systemPromptInterview, chatHistory, voiceId,
            request.PreviousFollowUpTarget, cancellationToken);

        await foreach (var chunk in audioChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }

        var pipelineMetrics = await pipelineTask.ConfigureAwait(false);

        turnSw.Stop();
        Console.WriteLine($"[INTERVIEW][TIMING] Turn total: {turnSw.ElapsedMilliseconds}ms");

        var metricsRow = new
        {
            schema = "interview.respond_to_turn.pipeline",
            interviewId = context.Interview.Id,
            correlationId = request.CorrelationId,
            competencyId = request.CompetencyId,
            phase = request.Phase,
            turnTotalMs = turnSw.ElapsedMilliseconds,
            firstTokenMs = pipelineMetrics.FirstTokenMs,
            markerDetectedMs = pipelineMetrics.MarkerDetectedMs,
            llmTotalMs = pipelineMetrics.LlmTotalMs,
            usedFallbackParse = pipelineMetrics.UsedFallbackParse,
            ttsFirstAudioMs = pipelineMetrics.TtsFirstAudioMs,
            ttsTotalMs = pipelineMetrics.TtsTotalMs,
            ttsSentenceCount = pipelineMetrics.TtsSentenceCount
        };
        Console.WriteLine($"[INTERVIEW][METRICS_ROW] {JsonSerializer.Serialize(metricsRow)}");
    }

    /// <summary>
    /// Runs the LLM streaming → sentence detection → TTS pipeline. Audio chunks are written
    /// to audioWriter concurrently via a drain task, so they flow to the HTTP response while
    /// the LLM is still generating tokens. Handles both [EVAL:...] lines (on-topic) and
    /// direct markers (edge cases) on line 1.
    /// </summary>
    private async Task<RespondToTurnPipelineMetrics> RunLlmPipelineAsync(
        ChannelWriter<byte[]> audioWriter,
        string systemPromptStatic,
        string? systemPromptInterview,
        List<ConversationTurn> chatHistory,
        string voiceId,
        string? previousFollowUpTarget,
        CancellationToken ct)
    {
        var metrics = new RespondToTurnPipelineMetrics();
        var sentenceChannel = Channel.CreateUnbounded<ChannelReader<byte[]>>();
        var drainTask = DrainTtsToChannelAsync(sentenceChannel.Reader, audioWriter, metrics, ct);

        try
        {
            var llmSw = Stopwatch.StartNew();
            long firstTokenMs = 0;
            long? markerDetectedAtMs = null;
            var fullResponse = new StringBuilder();
            var sentenceBuffer = new StringBuilder();
            bool markerDetected = false;
            TurnResponseMetadata? metadata = null;
            int sentenceCount = 0;
            Task<ElevenLabsTtsWebSocketManager>? preWarmTask = PreWarmTtsConnectionAsync(voiceId, ct);

            await foreach (var token in GatewayFacade.StreamAnthropicCompletionAsync(
                systemPromptStatic, chatHistory, ct, maxTokensOverride: 512,
                enablePromptCaching: true,
                systemPromptInterviewPart: systemPromptInterview).ConfigureAwait(false))
            {
                if (firstTokenMs == 0) firstTokenMs = llmSw.ElapsedMilliseconds;
                fullResponse.Append(token);

                if (!markerDetected)
                {
                    var accumulated = fullResponse.ToString();
                    var newlinePos = accumulated.IndexOf('\n');
                    if (newlinePos >= 0)
                    {
                        var firstLine = accumulated.Substring(0, newlinePos).Trim();

                        // Try [EVAL:action=X,result=Y,score=Z] first (on-topic response)
                        var evalMatch = EvalLineRegex.Match(firstLine);
                        if (evalMatch.Success)
                        {
                            markerDetected = true;
                            markerDetectedAtMs = llmSw.ElapsedMilliseconds;
                            var actionQuality = evalMatch.Groups[1].Value.ToLowerInvariant();
                            var resultQuality = evalMatch.Groups[2].Value.ToLowerInvariant();
                            var score = evalMatch.Groups[3].Success && int.TryParse(evalMatch.Groups[3].Value, out var s) ? s : 3;
                            score = Math.Max(1, Math.Min(5, score));

                            var evalResult = new HolisticEvaluationResult
                            {
                                CompetencyScore = score,
                                ActionQuality = actionQuality,
                                ResultQuality = resultQuality
                            };
                            InterviewRuntimeManager.EnforceFollowUpRules(evalResult, previousFollowUpTarget);

                            string responseType;
                            string? followUpTarget = null;
                            if (evalResult.FollowUpNeeded && evalResult.FollowUpTarget != null)
                            {
                                responseType = "follow_up";
                                followUpTarget = evalResult.FollowUpTarget;
                            }
                            else
                            {
                                responseType = "transition";
                            }

                            metadata = new TurnResponseMetadata
                            {
                                ResponseType = responseType,
                                FollowUpTarget = followUpTarget,
                                CompetencyScore = score,
                                ActionQuality = actionQuality,
                                ResultQuality = resultQuality
                            };
                            Console.WriteLine($"[INTERVIEW][TIMING] Marker detected at {llmSw.ElapsedMilliseconds}ms | eval: action={actionQuality},result={resultQuality},score={score} | decision={responseType}{(followUpTarget != null ? $":{followUpTarget}" : "")}");

                            await WriteEarlyMetadataAsync(audioWriter, metadata, ct).ConfigureAwait(false);

                            var afterMarker = accumulated.Substring(newlinePos + 1);
                            if (afterMarker.Length > 0)
                            {
                                sentenceBuffer.Append(afterMarker);
                                sentenceCount += QueueCompleteSentencesToChannel(sentenceBuffer, sentenceChannel.Writer, voiceId, ct, ref preWarmTask);
                            }
                            continue;
                        }

                        // Fall back to edge-case markers ([REPEAT], [TRANSITION], etc.)
                        var markerMatch = MarkerLineRegex.Match(firstLine);
                        if (markerMatch.Success)
                        {
                            markerDetected = true;
                            markerDetectedAtMs = llmSw.ElapsedMilliseconds;
                            metadata = BuildMetadataFromMatch(markerMatch);
                            Console.WriteLine($"[INTERVIEW][TIMING] Marker detected at {llmSw.ElapsedMilliseconds}ms | type={metadata.ResponseType}, target={metadata.FollowUpTarget ?? "n/a"}");

                            await WriteEarlyMetadataAsync(audioWriter, metadata, ct).ConfigureAwait(false);

                            var afterMarker = accumulated.Substring(newlinePos + 1);
                            if (afterMarker.Length > 0)
                            {
                                sentenceBuffer.Append(afterMarker);
                                sentenceCount += QueueCompleteSentencesToChannel(sentenceBuffer, sentenceChannel.Writer, voiceId, ct, ref preWarmTask);
                            }
                        }
                    }
                    continue;
                }

                sentenceBuffer.Append(token);
                sentenceCount += QueueCompleteSentencesToChannel(sentenceBuffer, sentenceChannel.Writer, voiceId, ct, ref preWarmTask);
            }
            llmSw.Stop();
            metrics.FirstTokenMs = firstTokenMs > 0 ? firstTokenMs : null;
            metrics.MarkerDetectedMs = markerDetectedAtMs;
            metrics.LlmTotalMs = llmSw.ElapsedMilliseconds;

            var remaining = sentenceBuffer.ToString().Trim();

            if (!markerDetected)
            {
                metrics.UsedFallbackParse = true;
                var rawResponse = fullResponse.ToString().Trim();
                metadata = ParseMetadata(rawResponse, previousFollowUpTarget);
                metadata.SpokenText = StripMarkersFromSpokenText(metadata.SpokenText);
                Console.WriteLine($"[INTERVIEW][TIMING] LLM (fallback parse): {llmSw.ElapsedMilliseconds}ms (first token: {firstTokenMs}ms, {rawResponse.Length} chars) | type={metadata.ResponseType}, target={metadata.FollowUpTarget ?? "n/a"}");

                if (!string.IsNullOrWhiteSpace(metadata.SpokenText))
                {
                    ElevenLabsTtsWebSocketManager? preWarmed = null;
                    if (preWarmTask != null)
                    {
                        try { preWarmed = await preWarmTask.ConfigureAwait(false); }
                        catch { /* pre-warm failed, fall back to fresh connection */ }
                        preWarmTask = null;
                    }

                    foreach (var sentence in SplitIntoSentences(metadata.SpokenText))
                    {
                        if (!string.IsNullOrWhiteSpace(sentence))
                        {
                            sentenceChannel.Writer.TryWrite(StartSentenceTts(sentence, voiceId, ct, preWarmed));
                            preWarmed = null;
                            sentenceCount++;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"[INTERVIEW][TIMING] LLM: {llmSw.ElapsedMilliseconds}ms (first token: {firstTokenMs}ms, {fullResponse.Length} chars) | type={metadata!.ResponseType}, target={metadata.FollowUpTarget ?? "n/a"}");

                remaining = StripMarkersFromSpokenText(remaining);
                if (!string.IsNullOrWhiteSpace(remaining))
                {
                    ElevenLabsTtsWebSocketManager? preWarmed = null;
                    if (preWarmTask != null)
                    {
                        try { preWarmed = await preWarmTask.ConfigureAwait(false); }
                        catch { /* pre-warm failed, fall back to fresh connection */ }
                        preWarmTask = null;
                    }
                    sentenceChannel.Writer.TryWrite(StartSentenceTts(remaining, voiceId, ct, preWarmed));
                    sentenceCount++;
                }
            }

            if (preWarmTask != null)
            {
                try { (await preWarmTask.ConfigureAwait(false)).Dispose(); } catch { }
            }

            sentenceChannel.Writer.Complete();
            await drainTask.ConfigureAwait(false);

            if (metadata != null && fullResponse.ToString().Contains("[END_INTERVIEW]"))
            {
                metadata.ResponseType = "end_interview";
                metadata.FollowUpTarget = null;
            }

            if (sentenceCount > 0)
            {
                string spokenText;
                if (markerDetected)
                {
                    var raw = fullResponse.ToString();
                    var nlPos = raw.IndexOf('\n');
                    spokenText = nlPos >= 0 ? StripMarkersFromSpokenText(raw.Substring(nlPos + 1)) : "";
                }
                else
                {
                    spokenText = StripMarkersFromSpokenText(metadata!.SpokenText);
                }

                FixTransitionFollowUpMismatch(metadata!, spokenText);

                var trailerJson = JsonSerializer.Serialize(new
                {
                    spokenText,
                    responseType = metadata?.ResponseType,
                    followUpTarget = metadata?.FollowUpTarget,
                    languageCode = metadata?.LanguageCode,
                    competencyScore = metadata?.CompetencyScore,
                    actionQuality = metadata?.ActionQuality,
                    resultQuality = metadata?.ResultQuality
                });
                var jsonBytes = Encoding.UTF8.GetBytes(trailerJson);
                var delimiter = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
                var trailerBytes = new byte[delimiter.Length + jsonBytes.Length];
                Buffer.BlockCopy(delimiter, 0, trailerBytes, 0, delimiter.Length);
                Buffer.BlockCopy(jsonBytes, 0, trailerBytes, delimiter.Length, jsonBytes.Length);
                await audioWriter.WriteAsync(trailerBytes, ct).ConfigureAwait(false);
            }

            return metrics;
        }
        catch (Exception ex)
        {
            sentenceChannel.Writer.TryComplete(ex);
            try { await drainTask.ConfigureAwait(false); } catch { }
            throw;
        }
        finally
        {
            audioWriter.TryComplete();
        }
    }

    /// Sends an early metadata signal to the frontend before any TTS audio, so the client
    /// can begin pre-generating the next competency question while transition audio plays.
    private static async Task WriteEarlyMetadataAsync(
        ChannelWriter<byte[]> audioWriter, TurnResponseMetadata metadata, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(new
        {
            responseType = metadata.ResponseType,
            followUpTarget = metadata.FollowUpTarget,
            early = true
        });
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var delimiter = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        var chunk = new byte[delimiter.Length + jsonBytes.Length];
        Buffer.BlockCopy(delimiter, 0, chunk, 0, delimiter.Length);
        Buffer.BlockCopy(jsonBytes, 0, chunk, delimiter.Length, jsonBytes.Length);
        await audioWriter.WriteAsync(chunk, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads per-sentence ChannelReaders in order and forwards audio chunks to audioWriter
    /// as they arrive from TTS. Each sentence's chunks stream through without waiting for
    /// the full sentence to complete, but ordering between sentences is preserved.
    /// </summary>
    private static async Task DrainTtsToChannelAsync(
        ChannelReader<ChannelReader<byte[]>> sentenceReader,
        ChannelWriter<byte[]> audioWriter,
        RespondToTurnPipelineMetrics metrics,
        CancellationToken ct)
    {
        var ttsTotalSw = Stopwatch.StartNew();
        long firstAudioChunkMs = 0;
        int sentenceIndex = 0;

        await foreach (var sentenceChunks in sentenceReader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            var sentSw = Stopwatch.StartNew();
            int chunkCount = 0;

            await foreach (var chunk in sentenceChunks.ReadAllAsync(ct).ConfigureAwait(false))
            {
                if (firstAudioChunkMs == 0)
                    firstAudioChunkMs = ttsTotalSw.ElapsedMilliseconds;
                await audioWriter.WriteAsync(chunk, ct).ConfigureAwait(false);
                chunkCount++;
            }

            sentSw.Stop();
            Console.WriteLine($"[INTERVIEW][TIMING] TTS sentence {sentenceIndex}: {sentSw.ElapsedMilliseconds}ms ({chunkCount} chunks)");
            sentenceIndex++;
        }

        ttsTotalSw.Stop();
        metrics.TtsFirstAudioMs = firstAudioChunkMs;
        metrics.TtsTotalMs = ttsTotalSw.ElapsedMilliseconds;
        metrics.TtsSentenceCount = sentenceIndex;
        if (sentenceIndex > 0)
            Console.WriteLine($"[INTERVIEW][TIMING] TTS total: {ttsTotalSw.ElapsedMilliseconds}ms (first audio: {firstAudioChunkMs}ms, {sentenceIndex} sentences)");
    }

    private int QueueCompleteSentencesToChannel(
        StringBuilder sentenceBuffer,
        ChannelWriter<ChannelReader<byte[]>> sentenceWriter,
        string voiceId,
        CancellationToken ct,
        ref Task<ElevenLabsTtsWebSocketManager>? preWarmTask)
    {
        int count = 0;
        while (true)
        {
            var buffered = sentenceBuffer.ToString();
            var end = SentenceBuffer.FindSentenceEnd(buffered);
            if (end < 0) break;

            var sentence = buffered.Substring(0, end + 1).Trim();
            sentenceBuffer.Remove(0, end + 1);
            sentence = StripMarkersFromSpokenText(sentence);
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                ElevenLabsTtsWebSocketManager? preWarmed = null;
                if (preWarmTask != null)
                {
                    if (preWarmTask.IsCompletedSuccessfully)
                    {
                        preWarmed = preWarmTask.Result;
                    }
                    preWarmTask = null;
                }
                sentenceWriter.TryWrite(StartSentenceTts(sentence, voiceId, ct, preWarmed));
                count++;
            }
        }
        return count;
    }

    private async Task<ElevenLabsTtsWebSocketManager> PreWarmTtsConnectionAsync(
        string voiceId, CancellationToken ct)
    {
        var manager = GatewayFacade.CreateElevenLabsTtsWebSocketManager();
        await manager.PreConnectAsync(voiceId, ElevenLabsTtsWebSocketManager.FormatMp3, ct).ConfigureAwait(false);
        return manager;
    }

    /// <summary>
    /// Kicks off TTS for a single sentence in the background. Returns a ChannelReader
    /// that yields audio chunks as they arrive from ElevenLabs, enabling the drain task
    /// to forward them to the HTTP response without waiting for the full sentence.
    /// </summary>
    private ChannelReader<byte[]> StartSentenceTts(
        string sentence, string voiceId, CancellationToken ct,
        ElevenLabsTtsWebSocketManager? preConnected = null)
    {
        var ch = Channel.CreateUnbounded<byte[]>();
        _ = Task.Run(async () =>
        {
            try
            {
                using var mgr = preConnected ?? GatewayFacade.CreateElevenLabsTtsWebSocketManager();
                await foreach (var chunk in mgr.SynthesizeBytesAsync(
                    sentence, voiceId, ElevenLabsTtsWebSocketManager.FormatMp3, ct).ConfigureAwait(false))
                {
                    await ch.Writer.WriteAsync(chunk, ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex) { ch.Writer.TryComplete(ex); return; }
            finally { ch.Writer.TryComplete(); }
        }, ct);
        return ch.Reader;
    }

    private static string BuildMergedPrompt(RespondToTurnRequest request)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(request.Language))
            sb.AppendLine($"LANGUAGE: Respond entirely in {request.Language}.");

        sb.AppendLine($"Competency: {request.CompetencyName}");
        sb.AppendLine($"Question asked: {request.CurrentQuestion}");
        sb.AppendLine($"Follow-up count: {request.FollowUpCount} of 2");
        sb.AppendLine($"Repeats remaining: {request.RepeatsRemaining}");
        if (!string.IsNullOrWhiteSpace(request.PreviousFollowUpTarget))
            sb.AppendLine($"Previous follow-up probed: {request.PreviousFollowUpTarget}");
        if (request.IsLastCompetency)
            sb.AppendLine("This is the LAST competency.");
        sb.AppendLine();
        sb.AppendLine("Candidate's response:");
        sb.AppendLine(request.CandidateTranscript);
        sb.AppendLine();
        sb.AppendLine("Apply the classification, evaluation, and follow-up rules from the system prompt.");

        return sb.ToString();
    }

    private static List<ConversationTurn> BuildChatHistory(RespondToTurnRequest request)
    {
        var history = new List<ConversationTurn>();

        if (!string.IsNullOrWhiteSpace(request.AccumulatedTranscript)
            && request.AccumulatedTranscript != request.CandidateTranscript)
        {
            history.Add(new ConversationTurn
            {
                Role = "user",
                Content = $"[Prior candidate responses for this competency]\n{request.AccumulatedTranscript}"
            });
        }

        if (!string.IsNullOrWhiteSpace(request.PreviousAiResponse))
        {
            history.Add(new ConversationTurn
            {
                Role = "assistant",
                Content = $"[Your previous response — do NOT repeat this phrasing]\n{request.PreviousAiResponse}"
            });
        }

        return history;
    }

    private static TurnResponseMetadata ParseMetadata(string rawResponse, string? previousFollowUpTarget = null)
    {
        var nlPos = rawResponse.IndexOf('\n');
        if (nlPos >= 0)
        {
            var firstLine = rawResponse.Substring(0, nlPos).Trim();

            // Try eval line first (with or without score)
            var evalMatch = EvalLineRegex.Match(firstLine);
            if (evalMatch.Success)
            {
                var actionQuality = evalMatch.Groups[1].Value.ToLowerInvariant();
                var resultQuality = evalMatch.Groups[2].Value.ToLowerInvariant();
                var score = evalMatch.Groups[3].Success && int.TryParse(evalMatch.Groups[3].Value, out var s) ? s : 3;
                score = Math.Max(1, Math.Min(5, score));

                var evalResult = new HolisticEvaluationResult
                {
                    CompetencyScore = score,
                    ActionQuality = actionQuality,
                    ResultQuality = resultQuality
                };
                InterviewRuntimeManager.EnforceFollowUpRules(evalResult, previousFollowUpTarget);

                string responseType;
                string? followUpTarget = null;
                if (evalResult.FollowUpNeeded && evalResult.FollowUpTarget != null)
                {
                    responseType = "follow_up";
                    followUpTarget = evalResult.FollowUpTarget;
                }
                else
                {
                    responseType = "transition";
                }

                var meta = new TurnResponseMetadata
                {
                    SpokenText = rawResponse.Substring(nlPos + 1).Trim(),
                    ResponseType = responseType,
                    FollowUpTarget = followUpTarget,
                    CompetencyScore = score,
                    ActionQuality = actionQuality,
                    ResultQuality = resultQuality
                };
                FixTransitionFollowUpMismatch(meta, meta.SpokenText);
                return meta;
            }

            var firstMatch = MarkerLineRegex.Match(firstLine);
            if (firstMatch.Success)
            {
                var meta = BuildMetadataFromMatch(firstMatch);
                meta.SpokenText = rawResponse.Substring(nlPos + 1).Trim();
                return meta;
            }
        }

        var lastMatch = MarkerLastRegex.Match(rawResponse);
        if (lastMatch.Success)
        {
            var meta = BuildMetadataFromMatch(lastMatch);
            meta.SpokenText = rawResponse[..lastMatch.Index].Trim();
            return meta;
        }

        return new TurnResponseMetadata
        {
            SpokenText = StripLeadingEvalLine(rawResponse),
            ResponseType = "transition",
            FollowUpTarget = null
        };
    }

    /// <summary>
    /// Guards against the LLM generating a follow-up question in its spoken text while
    /// the eval-based rules determined a transition (no follow-up). If the spoken text
    /// ends with a question mark but the response type is "transition", the candidate
    /// would hear a question but the system would immediately move on. Override to
    /// "follow_up" so the frontend waits for the candidate's answer.
    /// </summary>
    private static void FixTransitionFollowUpMismatch(TurnResponseMetadata metadata, string spokenText)
    {
        if (metadata.ResponseType != "transition") return;
        if (string.IsNullOrWhiteSpace(spokenText)) return;

        var trimmed = spokenText.TrimEnd();
        if (!trimmed.EndsWith('?')) return;

        string? inferredTarget = null;
        if (metadata.ActionQuality is "weak" or "missing")
            inferredTarget = "action";
        else if (metadata.ResultQuality is "weak" or "missing")
            inferredTarget = "result";

        metadata.ResponseType = "follow_up";
        metadata.FollowUpTarget = inferredTarget;
        Console.WriteLine($"[INTERVIEW] Overriding transition→follow_up: spoken text ends with '?' | target={inferredTarget ?? "null"}");
    }

    /// <summary>Removes a leading [EVAL:...] line from text so it is never sent as spoken content to the user.</summary>
    private static string StripLeadingEvalLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var match = LeadingEvalLineRegex.Match(text);
        return match.Success ? text.Substring(match.Length).Trim() : text.Trim();
    }

    /// <summary>
    /// Removes ALL known response markers from spoken text. Handles cases where the LLM
    /// outputs markers within the spoken portion (e.g. [END_INTERVIEW] after an [EVAL:...] line).
    /// </summary>
    private static string StripMarkersFromSpokenText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var cleaned = AnyMarkerRegex.Replace(text, "");
        cleaned = Regex.Replace(cleaned, @"\s*[\r\n]+\s*", " ");
        return cleaned.Trim();
    }

    private static TurnResponseMetadata BuildMetadataFromMatch(Match match)
    {
        var markerText = match.Value.Trim();
        string responseType;
        string? followUpTarget = null;
        string? languageCode = null;

        if (match.Groups[1].Success)
        {
            responseType = "follow_up";
            followUpTarget = match.Groups[1].Value;
        }
        else if (match.Groups[2].Success)
        {
            responseType = "language_switch";
            languageCode = match.Groups[2].Value;
        }
        else if (markerText.Contains("END_INTERVIEW"))
        {
            responseType = "end_interview";
        }
        else if (markerText.Contains("REPEAT"))
        {
            responseType = "repeat";
        }
        else if (markerText.Contains("OFF_TOPIC"))
        {
            responseType = "off_topic";
        }
        else
        {
            responseType = "transition";
        }

        return new TurnResponseMetadata
        {
            SpokenText = "",
            ResponseType = responseType,
            FollowUpTarget = followUpTarget,
            LanguageCode = languageCode
        };
    }

    private static List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var remaining = text;

        while (remaining.Length > 0)
        {
            var endIndex = SentenceBuffer.FindSentenceEnd(remaining);
            if (endIndex < 0)
            {
                var trimmed = remaining.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    sentences.Add(trimmed);
                break;
            }

            var sentence = remaining[..(endIndex + 1)].Trim();
            if (!string.IsNullOrWhiteSpace(sentence))
                sentences.Add(sentence);

            remaining = remaining[(endIndex + 1)..];
        }

        return sentences;
    }

    public void Dispose()
    {
        _gatewayFacade?.Dispose();
    }
}

/// <summary>
/// Request model for the streaming conversation turn endpoint.
/// </summary>
public class RespondToTurnRequest
{
    public string CandidateTranscript { get; set; } = "";
    public Guid CompetencyId { get; set; }
    public string CompetencyName { get; set; } = "";
    public string CurrentQuestion { get; set; } = "";
    public string Phase { get; set; } = "primary";
    public int FollowUpCount { get; set; }
    public string AccumulatedTranscript { get; set; } = "";
    public string? PreviousFollowUpTarget { get; set; }
    public int RepeatsRemaining { get; set; } = 2;
    /// <summary>ISO 639-1 code when the candidate has requested a language switch, null for default (English).</summary>
    public string? Language { get; set; }
    /// <summary>The AI's previous spoken response for this competency, used to avoid identical rephrasing on repeats.</summary>
    public string? PreviousAiResponse { get; set; }
    /// <summary>True when this is the final competency in the interview; prevents "let's move on" phrasing in the transition.</summary>
    public bool IsLastCompetency { get; set; }

    /// <summary>Optional client id to correlate FE and API timing logs.</summary>
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Metadata parsed from the LLM response, used to set HTTP headers before audio streaming.
/// </summary>
public class TurnResponseMetadata
{
    public string SpokenText { get; set; } = "";
    /// <summary>"follow_up", "transition", "repeat", "off_topic", "language_switch", or "end_interview"</summary>
    public string ResponseType { get; set; } = "transition";
    /// <summary>"action" or "result" when ResponseType is "follow_up", null otherwise</summary>
    public string? FollowUpTarget { get; set; }
    /// <summary>ISO 639-1 language code when ResponseType is "language_switch", null otherwise</summary>
    public string? LanguageCode { get; set; }
    /// <summary>Competency score (1-5) from eval line, null for edge-case responses</summary>
    public int? CompetencyScore { get; set; }
    /// <summary>"complete", "weak", or "missing" from eval line, null for edge-case responses</summary>
    public string? ActionQuality { get; set; }
    /// <summary>"complete", "weak", or "missing" from eval line, null for edge-case responses</summary>
    public string? ResultQuality { get; set; }
}
