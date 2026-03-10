using System.Text;
using System.Text.Json;

namespace Orchestrator.Domain;

/// <summary>
/// Orchestrates the AI interview flow: assembles the system prompt, walks through competencies,
/// evaluates responses holistically, generates targeted follow-ups, and records results.
/// Works with both real-time (phone) and text-based interview modes.
/// </summary>
internal sealed class InterviewRuntimeManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);

    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??=
        new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public InterviewRuntimeManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// Builds the full interview system prompt from the agent's structured behavioral fields,
    /// interview context, and universal rubric.
    /// </summary>
    public string BuildInterviewSystemPrompt(Agent agent, InterviewTemplate template, RoleTemplate role, string applicantName, string jobTitle)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"You are {agent.DisplayName}, an AI interviewer.");
        sb.AppendLine();

        var behavioralPrompt = AgentSystemPromptBuilder.Build(agent);
        if (!string.IsNullOrWhiteSpace(behavioralPrompt))
        {
            sb.AppendLine("## Behavioral Style");
            sb.AppendLine(behavioralPrompt);
            sb.AppendLine();
        }

        sb.AppendLine("## Interview Context");
        sb.AppendLine($"Role: {role.RoleName} ({role.Industry})");
        sb.AppendLine($"Candidate: {applicantName}");
        sb.AppendLine($"Position: {jobTitle}");
        sb.AppendLine();

        sb.AppendLine("## Response Guidelines");
        sb.AppendLine("You are conducting a structured interview. Respond naturally and conversationally.");
        sb.AppendLine("Do not use markdown, bullet points, or any text formatting.");
        sb.AppendLine("Speak as if talking to someone in person or on the phone.");
        sb.AppendLine("Ask one question at a time. Wait for the candidate to finish before responding.");
        sb.AppendLine();

        sb.AppendLine("## Scoring Framework — Behavioral Competency Scoring (Universal 1-5 Scale)");
        sb.AppendLine("Each competency is scored holistically on a 1-5 scale based on the overall quality of behavioral evidence.");
        sb.AppendLine("Scores are assigned after the candidate answers the primary question and any follow-ups.");
        sb.AppendLine();
        foreach (var level in UniversalRubric.GetAllLevels())
        {
            sb.AppendLine($"{level.Level} — {level.Label}: {level.Description}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Resolves template variables in opening/closing templates.
    /// </summary>
    public string ResolveTemplateVariables(string template, string applicantName, string agentName, string jobTitle)
    {
        return template
            .Replace("{{applicantName}}", applicantName)
            .Replace("{{agentName}}", agentName)
            .Replace("{{jobTitle}}", jobTitle);
    }

    /// <summary>
    /// Generates the primary interview question for a competency using the canonical example as a model
    /// (tone, length, framing), plus competency context, role/industry, and optional candidate context.
    /// </summary>
    public async Task<string> GeneratePrimaryQuestionAsync(
        string systemPrompt,
        Competency competency,
        string roleName,
        string industry,
        string? jobTitle = null,
        string? applicantName = null,
        string? candidateContext = null,
        bool includeTransition = false,
        string? previousCompetencyName = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrimaryQuestionPrompt(competency, roleName, industry, jobTitle, applicantName, candidateContext, includeTransition, previousCompetencyName);
        var history = new List<ConversationTurn> { new() { Role = "user", Content = prompt } };
        var response = await GatewayFacade.GenerateAnthropicCompletion(systemPrompt, history).ConfigureAwait(false);
        return response.Trim();
    }

    private string BuildPrimaryQuestionPrompt(
        Competency competency,
        string roleName,
        string industry,
        string? jobTitle,
        string? applicantName,
        string? candidateContext,
        bool includeTransition = false,
        string? previousCompetencyName = null)
    {
        var sb = new StringBuilder();

        if (includeTransition)
        {
            sb.AppendLine("You are transitioning from the previous competency to a new one.");
            if (!string.IsNullOrWhiteSpace(previousCompetencyName))
                sb.AppendLine($"The candidate just finished discussing: {previousCompetencyName}.");
            sb.AppendLine("Begin with a brief, warm one-sentence acknowledgment of the topic just discussed.");
            sb.AppendLine("Do NOT evaluate the candidate's answer (no 'great answer' or 'good job').");
            sb.AppendLine("Reference the previous topic by name, then naturally introduce the next question.");
            sb.AppendLine("Examples of good transitions:");
            sb.AppendLine("- \"Thanks for sharing your experience with [previous topic]. I'd like to ask you about something different now.\"");
            sb.AppendLine("- \"I appreciate you walking me through that example of [previous topic]. Let's move on to the next area.\"");
            sb.AppendLine();
        }

        sb.AppendLine("Generate exactly one behavioral interview question to ask the candidate for this competency.");
        sb.AppendLine("The question should invite the candidate to describe a specific past experience with concrete actions and outcomes.");
        sb.AppendLine("The question must feel natural and conversational, not templated. Do not read the example verbatim; use it only as a model for tone, length, and framing.");
        sb.AppendLine();
        sb.AppendLine($"Role: {roleName} ({industry})");
        if (!string.IsNullOrWhiteSpace(jobTitle))
            sb.AppendLine($"Position: {jobTitle}");
        if (!string.IsNullOrWhiteSpace(applicantName))
            sb.AppendLine($"Candidate name: {applicantName}");
        sb.AppendLine();
        sb.AppendLine($"Competency: {competency.Name}");
        if (!string.IsNullOrWhiteSpace(competency.Description))
            sb.AppendLine($"What we're evaluating: {competency.Description}");
        sb.AppendLine();
        sb.AppendLine("The question should naturally invite the candidate to describe:");
        sb.AppendLine("- The context or background of the experience");
        sb.AppendLine("- The concrete steps they took");
        sb.AppendLine("- The outcome and how they measured success");
        sb.AppendLine();
        sb.AppendLine("Example question (use as a model for style only, do not copy):");
        sb.AppendLine(string.IsNullOrWhiteSpace(competency.CanonicalExample)
            ? $"Tell me about a time when you demonstrated {competency.Name}."
            : $"\"{competency.CanonicalExample}\"");
        if (!string.IsNullOrWhiteSpace(candidateContext))
        {
            sb.AppendLine();
            sb.AppendLine("Optional context about the candidate (you may lightly personalize the question if relevant):");
            sb.AppendLine(candidateContext);
        }
        sb.AppendLine();
        if (includeTransition)
            sb.AppendLine("Respond with the transition acknowledgment followed by the question. Nothing else.");
        else
            sb.AppendLine("Respond with ONLY the single question to ask, nothing else. No quotes, no preamble.");
        return sb.ToString();
    }

    /// <summary>
    /// Loads the full interview context: template, role with competencies, agent, job, and applicant.
    /// </summary>
    public async Task<InterviewRuntimeContext?> LoadInterviewContextAsync(Interview interview)
    {
        if (!interview.InterviewTemplateId.HasValue)
            return null;

        var template = await DataFacade.GetInterviewTemplateById(interview.InterviewTemplateId.Value).ConfigureAwait(false);
        if (template == null || !template.RoleTemplateId.HasValue)
            return null;

        var role = await DataFacade.GetRoleTemplateById(template.RoleTemplateId.Value).ConfigureAwait(false);
        if (role == null)
            return null;

        var competencies = await DataFacade.GetCompetenciesByRoleTemplateId(role.Id).ConfigureAwait(false);
        competencies.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));

        var agent = template.AgentId.HasValue
            ? await DataFacade.GetAgentById(template.AgentId.Value).ConfigureAwait(false)
            : null;

        var job = await DataFacade.GetJobById(interview.JobId).ConfigureAwait(false);
        var applicant = await DataFacade.GetApplicantById(interview.ApplicantId).ConfigureAwait(false);

        return new InterviewRuntimeContext
        {
            Interview = interview,
            Template = template,
            Role = role,
            Competencies = competencies,
            Agent = agent,
            JobTitle = job?.Title ?? "the position",
            ApplicantName = applicant != null ? $"{applicant.FirstName} {applicant.LastName}".Trim() : "Candidate"
        };
    }

    /// <summary>
    /// Evaluates a candidate's full accumulated transcript for a competency.
    /// Assigns a 1-5 score, rationale, and determines follow-up needs via action/result quality.
    /// previousFollowUpTarget indicates what was already asked, enabling stricter re-evaluation rules.
    /// </summary>
    public async Task<HolisticEvaluationResult> EvaluateCompetencyResponseAsync(
        string systemPrompt,
        Competency competency,
        string competencyTranscript,
        string roleName,
        string industry,
        string? previousFollowUpTarget = null,
        CancellationToken cancellationToken = default)
    {
        var wordCount = competencyTranscript.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < 3)
        {
            return new HolisticEvaluationResult
            {
                CompetencyScore = 1,
                Rationale = "Response was too brief to evaluate.",
                FollowUpNeeded = true,
                FollowUpTarget = "action",
                ActionQuality = "missing",
                ResultQuality = "missing"
            };
        }

        var analysisPrompt = BuildEvaluationPrompt(competency, competencyTranscript, roleName, industry);

        var analysisHistory = new List<ConversationTurn>
        {
            new() { Role = "user", Content = analysisPrompt }
        };

        var analysisResponse = await GatewayFacade.GenerateAnthropicCompletion(
            systemPrompt, analysisHistory, temperatureOverride: 0.3
        ).ConfigureAwait(false);
        return ParseEvaluation(analysisResponse, previousFollowUpTarget);
    }

    /// <summary>
    /// Evaluates with full accumulated transcript. This overload exists for API compatibility
    /// but the transcript should already contain all prior exchanges concatenated.
    /// </summary>
    public async Task<HolisticEvaluationResult> EvaluateCompetencyResponseWithContextAsync(
        string systemPrompt,
        Competency competency,
        string competencyTranscript,
        string roleName,
        string industry,
        List<PriorExchange>? priorExchanges,
        string? previousFollowUpTarget = null,
        CancellationToken cancellationToken = default)
    {
        return await EvaluateCompetencyResponseAsync(
            systemPrompt, competency, competencyTranscript, roleName, industry, previousFollowUpTarget, cancellationToken
        ).ConfigureAwait(false);
    }

    private string BuildEvaluationPrompt(
        Competency competency,
        string competencyTranscript,
        string roleName,
        string industry)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a skilled behavioral interviewer evaluating a candidate's response.");
        sb.AppendLine();
        sb.AppendLine($"Role: {roleName} ({industry})");
        sb.AppendLine($"Competency: {competency.Name}");
        if (!string.IsNullOrWhiteSpace(competency.Description))
            sb.AppendLine($"Description: {competency.Description}");
        sb.AppendLine();

        sb.AppendLine("## Holistic Scoring (1-5)");
        sb.AppendLine("Evaluate the full accumulated response as a whole. Assign a single score 1-5:");
        sb.AppendLine("1 — No evidence: Vague or can't articulate a relevant example");
        sb.AppendLine("2 — Weak: Generic answer, no specifics, story is incomplete");
        sb.AppendLine("3 — Adequate: Real example with some specifics, story mostly complete");
        sb.AppendLine("4 — Strong: Concrete, specific, self-aware, complete story with clear actions and outcome");
        sb.AppendLine("5 — Exceptional: Specific process, demonstrates mastery, connects actions to measurable impact, fully complete story");
        sb.AppendLine();
        sb.AppendLine("Write a 2-3 sentence rationale in plain English referencing specific things the candidate said. The rationale must explain why this specific score was assigned.");
        sb.AppendLine();

        sb.AppendLine("## Evidence Quality Assessment");
        sb.AppendLine("Assess ONLY two components — Action and Result. Do NOT evaluate or follow up on Situation or Task.");
        sb.AppendLine();
        sb.AppendLine("action_quality — Do you know specifically what this person did? Not just that they acted, but the concrete steps they took.");
        sb.AppendLine("  complete — specific actions described clearly, even if brief (e.g. \"I inspected the oil, found metal, told the customer\" IS complete)");
        sb.AppendLine("  weak — actions mentioned but vague, lacking specificity (e.g. \"I helped them\" with no details)");
        sb.AppendLine("  missing — no meaningful description of what they did");
        sb.AppendLine();
        sb.AppendLine("IMPORTANT: A concise list of concrete steps counts as complete. Do NOT require lengthy elaboration if the candidate clearly stated what they did.");
        sb.AppendLine();
        sb.AppendLine("result_quality — Do you know what the outcome was? Not just that things worked out, but a concrete result. Evaluate the FULL accumulated transcript, not just the latest response.");
        sb.AppendLine("  complete — clear, specific outcome described at ANY point in the transcript (e.g. \"she became a return customer\", \"the issue was resolved\"). If the candidate already stated a result earlier, it is STILL complete — do NOT re-ask.");
        sb.AppendLine("  weak — outcome implied or vaguely mentioned (e.g. \"it worked out\")");
        sb.AppendLine("  missing — no outcome described anywhere in the accumulated transcript");
        sb.AppendLine();

        sb.AppendLine("## Follow-up Rules — apply in strict order, stop at first match:");
        sb.AppendLine("1. If competency_score is 4 or 5 → follow_up_needed: false, follow_up_target: null, follow_up_question: null");
        sb.AppendLine("2. If action_quality is complete AND result_quality is complete → follow_up_needed: false, follow_up_target: null, follow_up_question: null");
        sb.AppendLine("3. If action_quality is missing or weak → follow_up_needed: true, follow_up_target: \"action\"");
        sb.AppendLine("4. If action_quality is complete AND result_quality is missing or weak → follow_up_needed: true, follow_up_target: \"result\"");
        sb.AppendLine("NEVER set follow_up_target to \"situation\" or \"task\". Only \"action\" or \"result\" are valid targets.");
        sb.AppendLine();

        sb.AppendLine("## Follow-up Question Generation");
        sb.AppendLine("If follow_up_needed is true, you MUST generate a natural, conversational follow-up question.");
        sb.AppendLine("The question MUST:");
        sb.AppendLine("- Start by acknowledging or paraphrasing something the candidate actually said (use their words)");
        sb.AppendLine("- Then naturally ask for the missing evidence (action steps or outcome)");
        sb.AppendLine("- Sound like a real person talking, not a template or script");
        sb.AppendLine("- Be a single spoken sentence or two — no bullet points, no formatting");
        sb.AppendLine("- The follow-up MUST probe evidence of the COMPETENCY being assessed, not tangential procedural details.");
        sb.AppendLine("- IMPORTANT: If the candidate already DEMONSTRATED the competency through their actions (e.g. they noticed a detail AND acted on it), that IS behavioral evidence of the competency. Do NOT ask them to explain the internal mechanism of how they noticed/decided/felt — that is meta-cognitive introspection, not behavioral evidence. The fact that they caught it and responded appropriately is sufficient.");
        sb.AppendLine("- Do NOT ask for information the candidate already provided. If they already described concrete steps, do not re-ask \"what steps did you take\". If they already described a result, do not ask \"what was the outcome\". Instead, probe a different aspect or transition.");
        sb.AppendLine();
        sb.AppendLine("NEVER use generic questions like \"Can you walk me through exactly what steps you took?\" or \"What was the outcome?\"");
        sb.AppendLine("ALWAYS reference specific details from the candidate's answer to make the follow-up feel like a real conversation.");
        sb.AppendLine();
        sb.AppendLine("If follow_up_target is \"action\", the candidate hasn't described their concrete steps. Acknowledge what they shared, then ask specifically what they did.");
        sb.AppendLine("If follow_up_target is \"result\", the candidate hasn't described the outcome. Acknowledge the actions they described, then ask what happened as a result.");
        sb.AppendLine();
        sb.AppendLine("Examples of good follow-up questions (notice how each one references the candidate's specific words):");
        sb.AppendLine("- \"You mentioned you noticed metal shavings in the oil during the change — what did you do after you spotted that?\"");
        sb.AppendLine("- \"That's interesting that you reorganized the work order schedule for the whole team. What ended up happening after you made those changes?\"");
        sb.AppendLine("- \"You said you stepped in when the senior tech was out — can you walk me through what that actually looked like on a busy day?\"");
        sb.AppendLine("- \"It sounds like you put a lot of work into improving the tire rotation process. How did that turn out for the shop?\"");
        sb.AppendLine();
        sb.AppendLine("If follow_up_needed is false, set follow_up_question to null.");
        sb.AppendLine();

        sb.AppendLine("=== FULL ACCUMULATED CANDIDATE TRANSCRIPT ===");
        sb.AppendLine($"\"{competencyTranscript}\"");
        sb.AppendLine();

        sb.AppendLine("Respond in this exact JSON format (no markdown, no code fences, just raw JSON):");
        sb.AppendLine("{");
        sb.AppendLine("  \"competency_score\": <integer 1-5>,");
        sb.AppendLine("  \"rationale\": \"<2-3 sentence rationale referencing specific things the candidate said>\",");
        sb.AppendLine("  \"action_quality\": \"<complete|weak|missing>\",");
        sb.AppendLine("  \"result_quality\": \"<complete|weak|missing>\",");
        sb.AppendLine("  \"follow_up_needed\": <true or false>,");
        sb.AppendLine("  \"follow_up_target\": <\"action\" or \"result\" or null>,");
        sb.AppendLine("  \"follow_up_question\": <\"natural follow-up question referencing what the candidate said\" or null>");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private HolisticEvaluationResult ParseEvaluation(string aiResponse, string? previousFollowUpTarget = null)
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

                result.CompetencyScore = ClampScore(GetIntOrDefault(root, "competency_score", 1));

                result.Rationale = root.TryGetProperty("rationale", out var rat)
                    && rat.ValueKind == JsonValueKind.String ? rat.GetString() ?? "" : "";

                result.ActionQuality = root.TryGetProperty("action_quality", out var aq)
                    && aq.ValueKind == JsonValueKind.String ? aq.GetString() ?? "missing" : "missing";

                result.ResultQuality = root.TryGetProperty("result_quality", out var rq)
                    && rq.ValueKind == JsonValueKind.String ? rq.GetString() ?? "missing" : "missing";

                result.FollowUpNeeded = root.TryGetProperty("follow_up_needed", out var fu)
                    && fu.ValueKind == JsonValueKind.True;

                result.FollowUpTarget = root.TryGetProperty("follow_up_target", out var ft)
                    && ft.ValueKind == JsonValueKind.String ? ft.GetString() : null;

                result.FollowUpQuestion = root.TryGetProperty("follow_up_question", out var fq)
                    && fq.ValueKind == JsonValueKind.String ? fq.GetString() : null;

                EnforceFollowUpRules(result, previousFollowUpTarget);
            }
        }
        catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
        {
            Console.WriteLine($"Failed to parse evaluation JSON: {ex.Message}. Raw response: {aiResponse}");
            result.CompetencyScore = 1;
            result.Rationale = "Evaluation parsing failed.";
            result.FollowUpNeeded = true;
            result.FollowUpTarget = "action";
            result.ActionQuality = "missing";
            result.ResultQuality = "missing";
        }

        return result;
    }

    /// <summary>
    /// Enforces follow-up rules. When previousFollowUpTarget is provided (re-evaluation after a follow-up),
    /// applies stricter logic: if the previously targeted component didn't improve, finalize instead of repeating.
    /// </summary>
    internal static void EnforceFollowUpRules(HolisticEvaluationResult result, string? previousFollowUpTarget = null)
    {
        var originalTarget = result.FollowUpTarget;

        if (result.CompetencyScore >= 4)
        {
            result.FollowUpNeeded = false;
            result.FollowUpTarget = null;
            result.FollowUpQuestion = null;
            return;
        }

        if (result.ActionQuality == "complete" && result.ResultQuality == "complete")
        {
            result.FollowUpNeeded = false;
            result.FollowUpTarget = null;
            result.FollowUpQuestion = null;
            return;
        }

        if (previousFollowUpTarget == "action")
        {
            if (result.ActionQuality == "complete" && result.ResultQuality is "missing" or "weak")
            {
                result.FollowUpNeeded = true;
                result.FollowUpTarget = "result";
                if (originalTarget != "result") result.FollowUpQuestion = null;
                return;
            }

            result.FollowUpNeeded = false;
            result.FollowUpTarget = null;
            result.FollowUpQuestion = null;
            return;
        }

        if (previousFollowUpTarget == "result")
        {
            result.FollowUpNeeded = false;
            result.FollowUpTarget = null;
            result.FollowUpQuestion = null;
            return;
        }

        if (result.ActionQuality is "missing" or "weak")
        {
            result.FollowUpNeeded = true;
            result.FollowUpTarget = "action";
            if (originalTarget != "action") result.FollowUpQuestion = null;
            return;
        }

        if (result.ResultQuality is "missing" or "weak")
        {
            result.FollowUpNeeded = true;
            result.FollowUpTarget = "result";
            if (originalTarget != "result") result.FollowUpQuestion = null;
            return;
        }

        result.FollowUpNeeded = false;
        result.FollowUpTarget = null;
        result.FollowUpQuestion = null;
    }

    private static int GetIntOrDefault(JsonElement root, string property, int defaultValue)
    {
        if (!root.TryGetProperty(property, out var el)) return defaultValue;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var value)) return value;
        return defaultValue;
    }

    private static int ClampScore(int score) => Math.Max(1, Math.Min(5, score));

    /// <summary>
    /// Scores a completed competency and records the result with holistic competency score and rationale.
    /// Builds the competency_transcript from all candidate turns concatenated in order.
    /// </summary>
    public CompetencyResponse ScoreAndRecordCompetency(
        Competency competency,
        Guid interviewId,
        string primaryQuestion,
        string candidateResponse,
        List<FollowUpExchange>? followUpExchanges,
        HolisticEvaluationResult evaluation)
    {
        var questionsAsked = new List<string> { primaryQuestion };
        var transcriptParts = new List<string> { candidateResponse };

        if (followUpExchanges != null)
        {
            foreach (var exchange in followUpExchanges)
            {
                questionsAsked.Add(exchange.Question);
                transcriptParts.Add(exchange.Response);
            }
        }

        var competencyTranscript = string.Join("\n\n", transcriptParts);

        return new CompetencyResponse
        {
            InterviewId = interviewId,
            CompetencyId = competency.Id,
            CompetencyScore = evaluation.CompetencyScore,
            CompetencyRationale = evaluation.Rationale,
            FollowUpCount = followUpExchanges?.Count ?? 0,
            QuestionsAsked = JsonSerializer.Serialize(questionsAsked),
            GeneratedQuestionText = primaryQuestion,
            ResponseText = competencyTranscript,
            CompetencyTranscript = competencyTranscript,
            ScoringWeight = competency.DefaultWeight
        };
    }

    public void Dispose()
    {
        _gatewayFacade?.Dispose();
    }
}

/// <summary>
/// Full runtime context loaded for an interview session.
/// </summary>
public class InterviewRuntimeContext
{
    public Interview Interview { get; set; } = null!;
    public InterviewTemplate Template { get; set; } = null!;
    public RoleTemplate Role { get; set; } = null!;
    public List<Competency> Competencies { get; set; } = new();
    public Agent? Agent { get; set; }
    public string JobTitle { get; set; } = "";
    public string ApplicantName { get; set; } = "";
}

/// <summary>
/// Result of evaluating a candidate's response using holistic competency scoring.
/// action_quality, result_quality, follow_up_needed, and follow_up_target are transient —
/// used only to drive the state machine, never written to the database.
/// </summary>
public class HolisticEvaluationResult
{
    public int CompetencyScore { get; set; }
    public string Rationale { get; set; } = "";
    public string ActionQuality { get; set; } = "missing";
    public string ResultQuality { get; set; } = "missing";
    public bool FollowUpNeeded { get; set; }
    public string? FollowUpTarget { get; set; }
    public string? FollowUpQuestion { get; set; }

    /// <summary>
    /// Returns the AI-generated follow-up question, falling back to a static default if none was generated.
    /// </summary>
    public string? GetEffectiveFollowUpQuestion()
    {
        if (!string.IsNullOrWhiteSpace(FollowUpQuestion))
            return FollowUpQuestion;

        return GetFallbackFollowUpQuestion(FollowUpTarget);
    }

    /// <summary>
    /// Static fallback follow-up questions used only when the AI doesn't generate one.
    /// </summary>
    public static string? GetFallbackFollowUpQuestion(string? target)
    {
        return target?.ToLowerInvariant() switch
        {
            "action" => "Can you walk me through exactly what steps you took?",
            "result" => "What was the outcome of that, and how did you know it was successful?",
            _ => null
        };
    }
}

/// <summary>
/// A follow-up exchange collected during the conversational competency flow.
/// </summary>
public class FollowUpExchange
{
    public string Question { get; set; } = "";
    public string Response { get; set; } = "";
}

/// <summary>
/// A prior Q&A exchange provided as context for cumulative evaluation.
/// </summary>
public class PriorExchange
{
    public string Question { get; set; } = "";
    public string Response { get; set; } = "";
}
