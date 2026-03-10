using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
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
    private readonly ServiceLocatorBase _serviceLocator;
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);

    private static readonly Regex MetadataMarkerRegex = new(
        @"\[(?:FOLLOW_UP:(\w+)|TRANSITION|REPEAT|OFF_TOPIC|LANGUAGE_SWITCH:(\w+)|END_INTERVIEW)\]\s*$",
        RegexOptions.Compiled);

    public InterviewConversationManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// LATENCY-CRITICAL: Generates a conversational AI response and streams it as MP3 audio.
    /// 1. Streams OpenAI tokens and buffers the full response (~1-3 sentences)
    /// 2. Parses metadata marker ([FOLLOW_UP:target] or [TRANSITION])
    /// 3. Invokes onMetadataReady so the controller can set HTTP headers before audio body
    /// 4. Splits spoken text into sentences and synthesizes each via ElevenLabs WebSocket TTS
    /// 5. Yields raw MP3 byte chunks for direct HTTP response streaming
    /// </summary>
    public async IAsyncEnumerable<byte[]> RespondToTurnAsync(
        InterviewRuntimeContext context,
        RespondToTurnRequest request,
        Action<TurnResponseMetadata> onMetadataReady,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var systemPrompt = context.Agent != null
            ? new InterviewRuntimeManager(_serviceLocator).BuildInterviewSystemPrompt(
                context.Agent, context.Template, context.Role, context.ApplicantName, context.JobTitle)
            : "";

        // Fast eval gate: determine follow-up decision before generating the conversational response
        var evalResult = await QuickEvaluateAsync(context, request, cancellationToken).ConfigureAwait(false);
        string predeterminedDecision;
        if (evalResult.FollowUpNeeded && evalResult.FollowUpTarget != null)
            predeterminedDecision = $"follow_up:{evalResult.FollowUpTarget}";
        else
            predeterminedDecision = "transition";

        Console.WriteLine($"[INTERVIEW][EVAL] Quick eval: score={evalResult.CompetencyScore}, action={evalResult.ActionQuality}, result={evalResult.ResultQuality} → decision={predeterminedDecision}");

        var chatHistory = BuildChatHistory(request);
        var userMessage = BuildConversationalPrompt(request, predeterminedDecision);
        chatHistory.Add(new ConversationTurn { Role = "user", Content = userMessage });

        // LATENCY: Buffer full LLM response so we can parse metadata and set HTTP headers
        // before streaming audio. LLM output is 1-3 sentences (~300-600ms).
        var fullResponse = new StringBuilder();
        await foreach (var token in GatewayFacade.StreamAnthropicCompletionAsync(
            systemPrompt, chatHistory, cancellationToken).ConfigureAwait(false))
        {
            fullResponse.Append(token);
        }

        var rawResponse = fullResponse.ToString().Trim();
        var metadata = ParseMetadata(rawResponse);
        var spokenText = metadata.SpokenText;

        Console.WriteLine($"[INTERVIEW][CONV] Response: type={metadata.ResponseType}, target={metadata.FollowUpTarget ?? "n/a"}, text=\"{(spokenText.Length > 100 ? spokenText[..100] + "..." : spokenText)}\"");

        onMetadataReady(metadata);

        if (string.IsNullOrWhiteSpace(spokenText))
            yield break;

        var voiceId = context.Agent?.ElevenlabsVoiceId ?? "21m00Tcm4TlvDq8ikWAM";

        // LATENCY-CRITICAL: Split into sentences and synthesize each via WebSocket TTS.
        // Audio from sentence 1 streams to the browser while sentence 2 is being synthesized.
        var sentences = SplitIntoSentences(spokenText);
        foreach (var sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                continue;

            using var ttsManager = GatewayFacade.CreateElevenLabsTtsWebSocketManager();
            await foreach (var chunk in ttsManager.SynthesizeBytesAsync(
                sentence, voiceId, ElevenLabsTtsWebSocketManager.FormatMp3, cancellationToken).ConfigureAwait(false))
            {
                yield return chunk;
            }
        }
    }

    private static string BuildConversationalPrompt(RespondToTurnRequest request, string predeterminedDecision)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are responding to a candidate's answer in a behavioral interview.");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(request.Language))
            sb.AppendLine($"LANGUAGE: The candidate has requested to conduct this interview in {request.Language}. You MUST respond entirely in {request.Language}.");

        sb.AppendLine();
        sb.AppendLine($"Competency being assessed: {request.CompetencyName}");
        sb.AppendLine($"Question asked: {request.CurrentQuestion}");
        sb.AppendLine($"Follow-up count so far: {request.FollowUpCount} of 2 maximum");
        sb.AppendLine($"Repeats remaining: {request.RepeatsRemaining}");
        if (!string.IsNullOrWhiteSpace(request.PreviousFollowUpTarget))
            sb.AppendLine($"Previous follow-up probed: {request.PreviousFollowUpTarget}");
        sb.AppendLine();
        sb.AppendLine("Candidate's response:");
        sb.AppendLine(request.CandidateTranscript);
        sb.AppendLine();

        sb.AppendLine("## Step 1 — Classify the response");
        sb.AppendLine("Before responding, check if the response matches any of these edge cases (in order). Stop at the first match:");
        sb.AppendLine();
        sb.AppendLine("1. **Language switch request** — The candidate asks to switch languages (e.g. \"can we do this in Spanish?\", \"puedo hablar en español?\").");
        sb.AppendLine("   → Acknowledge warmly, restate the current question in the requested language, and append the marker.");
        sb.AppendLine("   → Marker: [LANGUAGE_SWITCH:xx] where xx is the ISO 639-1 code (e.g. es, fr, zh, pt).");
        sb.AppendLine();
        sb.AppendLine("2. **Repeat / clarification request** — The candidate asks you to repeat the question, says they don't understand, or asks what it means (e.g. \"can you say that again?\", \"what was the question?\", \"what do you mean by that?\", \"I'm not sure I understand\").");
        if (request.RepeatsRemaining > 0)
        {
            sb.AppendLine("   → Rephrase the question using DIFFERENT words and a DIFFERENT sentence structure. Do NOT repeat the previous phrasing. Use simpler, more conversational language and try a different angle to make the question easier to understand. Marker: [REPEAT]");
        }
        else
        {
            sb.AppendLine("   → You have already repeated this question the maximum number of times. Politely say you need to move forward and restate the question one final time. Marker: [REPEAT]");
        }
        sb.AppendLine();
        sb.AppendLine("3. **Nervous deflection** — The candidate stalls, expresses uncertainty, or gives a tangential non-answer that seems anxiety-driven (e.g. \"That's a good question, let me think...\", \"I've never really thought about that\", \"Hmm I'm not sure where to start\").");
        sb.AppendLine("   → Encourage warmly: \"Take your time — there's no rush. Just think of a specific situation where this came up for you at work, even a small one.\" Then briefly restate the question. Marker: [REPEAT]");
        sb.AppendLine();
        sb.AppendLine("4. **Process question** — The candidate asks about the interview process, data usage, recording, or who will see their answers (e.g. \"Is this being recorded?\", \"Who will see my answers?\", \"Why are you asking this?\").");
        sb.AppendLine("   → Answer honestly and briefly, then redirect: \"This interview is part of your application. Your responses help the hiring team understand your experience. Now, back to the question —\" and briefly restate the question. Marker: [REPEAT]");
        sb.AppendLine();
        sb.AppendLine("5. **Off-topic** — The response is completely unrelated to the interview question and not explained by nervousness (e.g. asking about the weather, unrelated personal questions, random topics).");
        sb.AppendLine("   → Politely redirect: \"I'm not able to help with that, but I'd love to hear your answer.\" Then briefly restate the question. Marker: [OFF_TOPIC]");
        sb.AppendLine();
        sb.AppendLine("6. **Disengagement / wants to end** — The candidate expresses frustration, fatigue, or a desire to stop the interview, end early, or skip the current question.");
        sb.AppendLine("   Apply these sub-rules:");
        sb.AppendLine("   a) **Explicit quit** — The candidate clearly says they want to stop or are done with the entire interview (e.g. \"I'm done with this interview\", \"I want to stop\", \"can we be done?\", \"I don't want to do this anymore\", \"end the interview\").");
        sb.AppendLine("      → Acknowledge warmly (e.g. \"I completely understand, and I really appreciate your time today. Thank you for participating.\"). Do NOT restate the question. Marker: [END_INTERVIEW]");
        sb.AppendLine("   b) **Frustration / skip** — The candidate expresses annoyance or wants to skip the current question but hasn't explicitly asked to end the whole interview (e.g. \"this is annoying\", \"let's just move on\", \"I'm over this question\", \"skip this one\").");
        sb.AppendLine("      → Acknowledge empathetically and briefly (e.g. \"I completely understand, and I appreciate your time. Let's move on.\"). Do NOT restate the question. Marker: [TRANSITION]");
        sb.AppendLine();
        sb.AppendLine("7. **Adversarial** — The candidate makes a political statement, accuses the AI of bias, objects to being interviewed by an AI, or attempts to manipulate the interview.");
        sb.AppendLine("   → De-escalate calmly. For bias accusations: \"I understand your concern. If you'd like to speak with someone from the hiring team directly, please contact them. I'm here to give you the opportunity to share your experience.\" For AI objections: \"I completely understand. You're welcome to contact the hiring team to arrange an alternative. If you'd like to continue, I'm ready when you are.\" For political requests: \"I'm not able to discuss that topic, but I'm here to learn more about your experience.\" Then briefly restate the question. Marker: [TRANSITION]");
        sb.AppendLine();
        sb.AppendLine("8. **Sensitive disclosure** — The candidate volunteers information about a protected characteristic, disability, medical condition, pregnancy, religion, or personal hardship that was NOT asked for.");
        sb.AppendLine("   → Acknowledge warmly but briefly (e.g. \"Thank you for sharing that.\"). Do NOT ask follow-up questions about the disclosure. Do NOT reference the disclosure again at any point. Restate the interview question. Marker: [REPEAT]");
        sb.AppendLine("   CRITICAL: Never repeat, summarize, or reference the content of a sensitive disclosure in any subsequent response.");
        sb.AppendLine();
        sb.AppendLine("9. **Tacit knowledge / can't elaborate** — The candidate indicates the behavior was automatic, instinctive, or they cannot explain the internal mechanism (e.g. \"I don't know, I just noticed it\", \"it's just something I do\", \"I can't really explain it\", \"that's just experience I guess\", \"I just saw it\", \"I don't know what you mean\").");
        sb.AppendLine("   → Acknowledge warmly (e.g. \"That makes sense — sounds like it's second nature to you at this point.\") and transition. Marker: [TRANSITION]");
        sb.AppendLine();
        sb.AppendLine("10. **Pushback / refusal to elaborate** — The candidate indicates they have already answered, refuses to provide more detail, or expresses dismissive frustration at being re-asked (e.g. \"I already told you\", \"I just said that\", \"I feel like I already covered that\", \"I feel like I just told you that\", \"I already answered that\", \"I don't have anything else to add\", \"obviously\", \"that's silly\", \"that's a dumb question\", \"you already know that\", \"I literally just said that\", \"what do you think happened\").");
        sb.AppendLine("   → Acknowledge politely (e.g. \"I appreciate your answer, thank you.\") and transition. Marker: [TRANSITION]");
        sb.AppendLine();
        sb.AppendLine("If none of the above match, the response is **on-topic**. Proceed to Step 2.");
        sb.AppendLine();

        sb.AppendLine("## Step 2 — Generate response (on-topic responses only)");
        sb.AppendLine("The candidate's response has already been evaluated by a separate system. You MUST follow its predetermined decision exactly.");
        sb.AppendLine($"**Predetermined decision: {predeterminedDecision}**");
        sb.AppendLine();

        if (predeterminedDecision == "transition")
        {
            if (request.IsLastCompetency)
            {
                sb.AppendLine("The answer is sufficient. Briefly and warmly acknowledge the candidate's answer. Do NOT mention moving to another question or suggest there are more questions coming.");
            }
            else
            {
                sb.AppendLine("The answer is sufficient. Briefly acknowledge the candidate's answer with warmth and move on.");
            }
            sb.AppendLine("Marker: [TRANSITION]");
        }
        else if (predeterminedDecision == "follow_up:action" && request.FollowUpCount < 2)
        {
            sb.AppendLine("The candidate hasn't described specific steps they took. Your job is to generate a natural follow-up question that asks what they specifically did.");
            sb.AppendLine();
            sb.AppendLine("Follow-up requirements:");
            sb.AppendLine("- Start by acknowledging or paraphrasing something the candidate said (use their words)");
            sb.AppendLine("- Then naturally ask for the missing evidence — what concrete steps they took");
            sb.AppendLine("- NEVER use generic questions like \"Can you walk me through the steps?\" — always reference their specific answer");
            sb.AppendLine("- The follow-up MUST probe evidence of the COMPETENCY being assessed, not tangential procedural details");
            sb.AppendLine("- IMPORTANT: If the candidate already DEMONSTRATED the competency through their actions (e.g. they noticed a detail AND acted on it), that IS behavioral evidence. Do NOT ask them to explain the internal mechanism of how they noticed/decided/felt — that is meta-cognitive introspection, not behavioral evidence.");
            sb.AppendLine("- Do NOT ask for information the candidate already provided in this response OR any earlier response");
            sb.AppendLine("- Keep it to 1-2 natural sentences");
            sb.AppendLine("Marker: [FOLLOW_UP:action]");
        }
        else if (predeterminedDecision == "follow_up:result" && request.FollowUpCount < 2)
        {
            sb.AppendLine("The candidate hasn't described the outcome. Your job is to generate a natural follow-up question that asks what happened as a result.");
            sb.AppendLine();
            sb.AppendLine("Follow-up requirements:");
            sb.AppendLine("- Start by acknowledging or paraphrasing the actions the candidate described (use their words)");
            sb.AppendLine("- Then naturally ask what the outcome or result was");
            sb.AppendLine("- NEVER use generic questions like \"What was the outcome?\" — always reference specific details from their answer");
            sb.AppendLine("- Do NOT ask for information the candidate already provided in this response OR any earlier response");
            sb.AppendLine("- Keep it to 1-2 natural sentences");
            sb.AppendLine("Marker: [FOLLOW_UP:result]");
        }
        else
        {
            if (request.IsLastCompetency)
                sb.AppendLine("Maximum follow-ups reached or no further follow-up is possible. Briefly and warmly acknowledge the candidate's answer. Do NOT mention moving to another question.");
            else
                sb.AppendLine("Maximum follow-ups reached or no further follow-up is possible. Briefly acknowledge and move on.");
            sb.AppendLine("Marker: [TRANSITION]");
        }

        sb.AppendLine();
        sb.AppendLine("## Output rules");
        sb.AppendLine("- Your spoken response MUST be natural conversational speech — no markdown, no bullet points");
        sb.AppendLine("- Keep it to 1-3 sentences maximum");
        sb.AppendLine("- The metadata marker MUST be on its own line at the very end");
        sb.AppendLine("- Do NOT include the marker text in the spoken portion");
        sb.AppendLine("- Exactly ONE marker per response. Valid markers: [FOLLOW_UP:action], [FOLLOW_UP:result], [TRANSITION], [REPEAT], [OFF_TOPIC], [LANGUAGE_SWITCH:xx], [END_INTERVIEW]");

        return sb.ToString();
    }

    /// <summary>
    /// LATENCY-OPTIMIZED: Runs a fast evaluation of the candidate's response using Haiku
    /// to determine action/result quality and competency score. Returns a result with
    /// EnforceFollowUpRules already applied so the caller gets a deterministic decision.
    /// </summary>
    private async Task<HolisticEvaluationResult> QuickEvaluateAsync(
        InterviewRuntimeContext context,
        RespondToTurnRequest request,
        CancellationToken cancellationToken)
    {
        var prompt = BuildQuickEvalPrompt(request, context);
        var history = new List<ConversationTurn> { new() { Role = "user", Content = prompt } };
        var response = await GatewayFacade.GenerateAnthropicCompletion(
            "", history,
            modelOverride: "claude-haiku-4-5",
            temperatureOverride: 0.2,
            maxTokensOverride: 256
        ).ConfigureAwait(false);
        return ParseQuickEval(response, request.PreviousFollowUpTarget);
    }

    private static string BuildQuickEvalPrompt(RespondToTurnRequest request, InterviewRuntimeContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are evaluating a candidate's behavioral interview response. Return ONLY a JSON object.");
        sb.AppendLine();
        sb.AppendLine($"Competency: {request.CompetencyName}");
        sb.AppendLine($"Role: {context.Role.RoleName} ({context.Role.Industry})");
        sb.AppendLine($"Question asked: {request.CurrentQuestion}");
        if (!string.IsNullOrWhiteSpace(request.PreviousFollowUpTarget))
            sb.AppendLine($"Previous follow-up probed: {request.PreviousFollowUpTarget}");
        sb.AppendLine();

        sb.AppendLine("=== FULL ACCUMULATED CANDIDATE TRANSCRIPT ===");
        sb.AppendLine($"\"{request.AccumulatedTranscript}\"");
        sb.AppendLine();

        sb.AppendLine("## Scoring (1-5)");
        sb.AppendLine("1 — No evidence: Vague or can't articulate a relevant example");
        sb.AppendLine("2 — Weak: Generic answer, no specifics, story is incomplete");
        sb.AppendLine("3 — Adequate: Real example with some specifics, story mostly complete");
        sb.AppendLine("4 — Strong: Concrete, specific, self-aware, complete story with clear actions and outcome");
        sb.AppendLine("5 — Exceptional: Specific process, demonstrates mastery, connects actions to measurable impact");
        sb.AppendLine();

        sb.AppendLine("## Evidence Assessment — Action and Result ONLY");
        sb.AppendLine("action_quality — Did the candidate describe specific, concrete steps THEY personally took?");
        sb.AppendLine("  complete — Specific actions described clearly, even if brief (e.g. \"I inspected the oil, found metal, told the customer\")");
        sb.AppendLine("  weak — Actions mentioned but vague (e.g. \"I helped them\")");
        sb.AppendLine("  missing — No meaningful description of what they did");
        sb.AppendLine("A concise list of concrete steps counts as complete. Do NOT require lengthy elaboration.");
        sb.AppendLine();
        sb.AppendLine("result_quality — Did the candidate describe a clear outcome? Check the FULL transcript, not just the latest response.");
        sb.AppendLine("  complete — Clear, specific outcome at ANY point (e.g. \"she became a return customer\", \"the issue was resolved\")");
        sb.AppendLine("  weak — Outcome vaguely implied (e.g. \"it worked out\")");
        sb.AppendLine("  missing — No outcome described anywhere in the transcript");
        sb.AppendLine();

        sb.AppendLine("Respond with ONLY this JSON (no markdown, no explanation):");
        sb.AppendLine("{\"competency_score\":<1-5>,\"action_quality\":\"<complete|weak|missing>\",\"result_quality\":\"<complete|weak|missing>\"}");

        return sb.ToString();
    }

    private static HolisticEvaluationResult ParseQuickEval(string aiResponse, string? previousFollowUpTarget)
    {
        var result = new HolisticEvaluationResult();

        try
        {
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("competency_score", out var sc) && sc.ValueKind == JsonValueKind.Number && sc.TryGetInt32(out var score))
                    result.CompetencyScore = Math.Max(1, Math.Min(5, score));
                else
                    result.CompetencyScore = 1;

                result.ActionQuality = root.TryGetProperty("action_quality", out var aq) && aq.ValueKind == JsonValueKind.String
                    ? aq.GetString() ?? "missing" : "missing";
                result.ResultQuality = root.TryGetProperty("result_quality", out var rq) && rq.ValueKind == JsonValueKind.String
                    ? rq.GetString() ?? "missing" : "missing";
            }
        }
        catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
        {
            Console.WriteLine($"[INTERVIEW][EVAL] Failed to parse quick eval JSON: {ex.Message}. Raw: {aiResponse}");
            result.CompetencyScore = 1;
            result.ActionQuality = "missing";
            result.ResultQuality = "missing";
        }

        InterviewRuntimeManager.EnforceFollowUpRules(result, previousFollowUpTarget);
        return result;
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

    private static TurnResponseMetadata ParseMetadata(string rawResponse)
    {
        var match = MetadataMarkerRegex.Match(rawResponse);
        if (!match.Success)
        {
            return new TurnResponseMetadata
            {
                SpokenText = rawResponse,
                ResponseType = "transition",
                FollowUpTarget = null
            };
        }

        var spokenText = rawResponse[..match.Index].Trim();
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
            SpokenText = spokenText,
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
}
