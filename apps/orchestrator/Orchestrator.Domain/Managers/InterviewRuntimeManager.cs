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
        var (staticPart, interviewPart) = BuildInterviewSystemPromptParts(agent, template, role, applicantName, jobTitle);
        return staticPart + interviewPart;
    }

    /// <summary>
    /// Returns the system prompt split into two parts for multi-breakpoint prompt caching.
    /// The static part (rules, rubric, guidelines) is identical across all interviews and
    /// cached as a prefix. The interview part (agent identity, context) varies per interview.
    /// Static content MUST come first so Anthropic's prefix cache can reuse it across interviews.
    /// </summary>
    public (string StaticPart, string InterviewPart) BuildInterviewSystemPromptParts(
        Agent agent, InterviewTemplate template, RoleTemplate role, string applicantName, string jobTitle)
    {
        var staticSb = new StringBuilder();

        staticSb.AppendLine("## Response Guidelines");
        staticSb.AppendLine("You are conducting a structured interview. Respond naturally and conversationally.");
        staticSb.AppendLine("Do not use markdown, bullet points, or any text formatting.");
        staticSb.AppendLine("Speak as if talking to someone in person or on the phone.");
        staticSb.AppendLine("Ask one question at a time. Wait for the candidate to finish before responding.");
        staticSb.AppendLine();

        staticSb.AppendLine("## Speech Pacing (MANDATORY)");
        staticSb.AppendLine("Your spoken responses are streamed sentence-by-sentence to text-to-speech.");
        staticSb.AppendLine("The FIRST sentence you speak in every turn MUST be exactly 3 to 5 words. No exceptions.");
        staticSb.AppendLine("This short opener lets audio playback start instantly while you generate the rest.");
        staticSb.AppendLine();
        staticSb.AppendLine("First-sentence patterns (vary these — NEVER repeat the same opener twice in a row):");
        staticSb.AppendLine("- Acknowledge a detail: \"Smart move on that.\" / \"Good eye catching that.\" / \"Solid work there.\"");
        staticSb.AppendLine("- React naturally: \"That makes total sense.\" / \"Fair enough on that.\" / \"Interesting approach there.\"");
        staticSb.AppendLine("- Affirm briefly: \"Right, got it.\" / \"Makes sense to me.\" / \"That tracks, yeah.\"");
        staticSb.AppendLine();
        staticSb.AppendLine("Put the follow-up question or additional comment in the SECOND sentence.");
        staticSb.AppendLine();

        staticSb.AppendLine("## Scoring Framework — Behavioral Competency Scoring (Universal 1-5 Scale)");
        staticSb.AppendLine("Each competency is scored holistically on a 1-5 scale based on the overall quality of behavioral evidence.");
        staticSb.AppendLine("Scores are assigned after the candidate answers the primary question and any follow-ups.");
        staticSb.AppendLine();
        foreach (var level in UniversalRubric.GetAllLevels())
        {
            staticSb.AppendLine($"{level.Level} — {level.Label}: {level.Description}");
        }

        staticSb.AppendLine();
        staticSb.Append(AppendTurnResponseReferenceForCaching());

        var interviewSb = new StringBuilder();

        interviewSb.AppendLine($"You are {agent.DisplayName}, an AI interviewer.");
        interviewSb.AppendLine();

        var behavioralPrompt = AgentSystemPromptBuilder.Build(agent);
        if (!string.IsNullOrWhiteSpace(behavioralPrompt))
        {
            interviewSb.AppendLine("## Behavioral Style");
            interviewSb.AppendLine(behavioralPrompt);
            interviewSb.AppendLine();
        }

        interviewSb.AppendLine("## Interview Context");
        interviewSb.AppendLine($"Role: {role.RoleName} ({role.Industry})");
        interviewSb.AppendLine($"Candidate: {applicantName}");
        interviewSb.AppendLine($"Position: {jobTitle}");

        return (staticSb.ToString(), interviewSb.ToString());
    }

    /// <summary>
    /// Full authoritative instructions for responding to candidate turns.
    /// Cached in the system prompt so Anthropic prompt caching covers all static rules.
    /// Per-turn specifics (competency, transcript, follow-up count, etc.) remain in the user message.
    /// </summary>
    private static string AppendTurnResponseReferenceForCaching()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Responding to candidate turns");
        sb.AppendLine("When the user message contains a candidate response, you must BOTH classify and respond in a single output.");
        sb.AppendLine();

        // ── Step 1: Edge-case classification (full version with examples) ──
        sb.AppendLine("### Step 1 — Classify the response");
        sb.AppendLine("Before responding, check if the response matches any of these edge cases (in order). Stop at the first match:");
        sb.AppendLine();
        sb.AppendLine("1. **Language switch request** — The candidate asks to switch languages (e.g. \"can we do this in Spanish?\", \"puedo hablar en español?\").");
        sb.AppendLine("   → Acknowledge warmly, restate the current question in the requested language, and append the marker.");
        sb.AppendLine("   → Marker: [LANGUAGE_SWITCH:xx] where xx is the ISO 639-1 code (e.g. es, fr, zh, pt).");
        sb.AppendLine();
        sb.AppendLine("2. **Repeat / clarification request** — The candidate asks to repeat or doesn't understand (e.g. \"can you say that again?\", \"what do you mean?\").");
        sb.AppendLine("   → If repeats_remaining > 0: Rephrase using DIFFERENT words and structure. Do NOT repeat previous phrasing. Marker: [REPEAT]");
        sb.AppendLine("   → If repeats_remaining = 0: Politely say you need to move forward and restate one final time. Marker: [REPEAT]");
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
        sb.AppendLine("6. **Disengagement / wants to end** — Frustration, fatigue, or desire to stop/skip.");
        sb.AppendLine("   a) **Explicit quit** (e.g. \"I'm done\", \"I want to stop\", \"end the interview\") → Acknowledge warmly, do NOT restate the question. Marker: [END_INTERVIEW]");
        sb.AppendLine("   b) **Frustration / skip** (e.g. \"let's move on\", \"skip this one\") → Acknowledge briefly, do NOT restate the question. Marker: [TRANSITION]");
        sb.AppendLine();
        sb.AppendLine("7. **Adversarial** — Political statements, bias accusations, AI objections, or manipulation attempts.");
        sb.AppendLine("   → De-escalate calmly, offer contact with hiring team if needed, restate the question. Marker: [TRANSITION]");
        sb.AppendLine();
        sb.AppendLine("8. **Sensitive disclosure** — The candidate volunteers information about a protected characteristic, disability, medical condition, pregnancy, religion, or personal hardship that was NOT asked for.");
        sb.AppendLine("   → Acknowledge warmly but briefly (e.g. \"Thank you for sharing that.\"). Do NOT ask follow-up questions about the disclosure. Do NOT reference the disclosure again at any point. Restate the interview question. Marker: [REPEAT]");
        sb.AppendLine("   CRITICAL: Never repeat, summarize, or reference the content of a sensitive disclosure in any subsequent response.");
        sb.AppendLine();
        sb.AppendLine("9. **Tacit knowledge / can't elaborate** — The candidate indicates the behavior was automatic, instinctive, or they cannot explain the internal mechanism (e.g. \"I don't know, I just noticed it\", \"it's just something I do\", \"I can't really explain it\", \"that's just experience I guess\", \"I just saw it\", \"I don't know what you mean\").");
        sb.AppendLine("   → Acknowledge warmly (e.g. \"That makes sense — sounds like it's second nature to you at this point.\") and transition. Marker: [TRANSITION]");
        sb.AppendLine();
        sb.AppendLine("10. **Pushback / refusal to elaborate** — The candidate says they already answered or refuses to add more (e.g. \"I already told you\", \"I don't have anything else to add\", \"that's a dumb question\").");
        sb.AppendLine("   → Acknowledge politely and transition. Marker: [TRANSITION]");
        sb.AppendLine();
        sb.AppendLine("If none of the above match, the response is **on-topic**. Proceed to Step 2.");
        sb.AppendLine();

        // ── Step 2: Evaluate evidence + respond ──
        sb.AppendLine("### Step 2 — Evaluate evidence and respond (on-topic only)");
        sb.AppendLine();
        sb.AppendLine("Assess the candidate's accumulated response for this competency.");
        sb.AppendLine();
        sb.AppendLine("**action_quality** — Did the candidate describe specific, concrete steps THEY personally took?");
        sb.AppendLine("  complete — Specific actions described clearly, even if brief (e.g. \"I inspected the oil, found metal, told the customer\"). If the candidate named 2+ specific things they did, that is complete.");
        sb.AppendLine("  weak — Actions mentioned but vague (e.g. \"I helped them\")");
        sb.AppendLine("  missing — No meaningful description of what they did");
        sb.AppendLine();
        sb.AppendLine("**result_quality** — Did the candidate describe a clear outcome? Check the FULL accumulated transcript, not just the latest response.");
        sb.AppendLine("  complete — Clear, specific outcome at ANY point (e.g. \"she became a return customer\", \"the issue was resolved\")");
        sb.AppendLine("  weak — Outcome vaguely implied (e.g. \"it worked out\")");
        sb.AppendLine("  missing — No outcome described anywhere in the transcript");
        sb.AppendLine();
        sb.AppendLine("**score** (1-5): Use the Scoring Framework rubric above.");
        sb.AppendLine("If both action and result are complete, score should be at least 3.");
        sb.AppendLine();
        sb.AppendLine("Line 1 of your output MUST be exactly: [EVAL:action=<complete|weak|missing>,result=<complete|weak|missing>,score=<1-5>]");
        sb.AppendLine("When the response is on-topic (Step 2), the **first character** of your entire output must be `[` (start of `[EVAL:`). Do not output spaces, newlines, preamble, or any other text before that line.");
        sb.AppendLine();

        // ── Follow-up decision branches (model selects based on user-message state) ──
        sb.AppendLine("### Follow-up decision rules");
        sb.AppendLine("The user message provides follow_up_count, previous_follow_up, and is_last_competency. Apply the FIRST matching branch:");
        sb.AppendLine();
        sb.AppendLine("**BRANCH_MAX_REACHED** (follow_up_count >= 2):");
        sb.AppendLine("Maximum follow-ups already reached. Acknowledge warmly and move on.");
        sb.AppendLine();
        sb.AppendLine("**BRANCH_RESULT_ASKED** (previous_follow_up = \"result\"):");
        sb.AppendLine("A follow-up on result was already asked. Regardless of the assessment, acknowledge warmly and move on.");
        sb.AppendLine();
        sb.AppendLine("**BRANCH_ACTION_ASKED** (previous_follow_up = \"action\"):");
        sb.AppendLine("A follow-up on action was already asked. Apply these rules:");
        sb.AppendLine("- If action is now complete BUT result is weak or missing → ask a follow-up about the outcome/result.");
        sb.AppendLine("- Otherwise → acknowledge warmly and move on (do not ask another follow-up).");
        sb.AppendLine();
        sb.AppendLine("**BRANCH_DEFAULT** (no previous follow-up):");
        sb.AppendLine("- If score >= 4 → acknowledge warmly.");
        sb.AppendLine("- If action=complete AND result=complete → acknowledge warmly.");
        sb.AppendLine("- If action is weak or missing → ask a follow-up about what they specifically did.");
        sb.AppendLine("- If result is weak or missing → ask a follow-up about the outcome.");
        sb.AppendLine("- Otherwise → acknowledge warmly.");
        sb.AppendLine();

        // ── Transition guidance ──
        sb.AppendLine("### Transition guidance");
        sb.AppendLine("**When acknowledging (transition):**");
        sb.AppendLine("- One short sentence (2-4 words) warmly acknowledging one specific detail from their answer.");
        sb.AppendLine("- If is_last_competency = true: Do NOT mention moving to another question or suggest there are more questions coming.");
        sb.AppendLine("- If is_last_competency = false: Do NOT say phrases like \"let's move on\", \"let's go to the next question\", or any forward-looking transition.");
        sb.AppendLine();

        // ── Follow-up guidance ──
        sb.AppendLine("### Follow-up guidance");
        sb.AppendLine("**When following up on action:** Acknowledge something the candidate said, then ask what concrete steps they took.");
        sb.AppendLine("**When following up on result:** Acknowledge the actions described, then ask what the outcome was.");
        sb.AppendLine();
        sb.AppendLine("- ALWAYS reference their specific answer — never use generic questions");
        sb.AppendLine("- Probe evidence of the COMPETENCY, not tangential details. Demonstrated behavior IS evidence — do NOT ask them to explain how they noticed/decided/felt.");
        sb.AppendLine("- Do NOT re-ask for information already provided. Keep to 1-2 natural sentences.");
        sb.AppendLine();
        sb.AppendLine("Example: \"You mentioned metal shavings in the oil — what did you do after you spotted that?\"");
        sb.AppendLine();

        // ── Output rules ──
        sb.AppendLine("### Output rules");
        sb.AppendLine("- Line 1: the [EVAL:...] tag (for on-topic) or an edge-case marker, alone on the first line");
        sb.AppendLine("- Line 2+: your spoken response — natural conversational speech, no markdown, no bullet points");
        sb.AppendLine("- First sentence after line 1: 2-4 words ONLY (see Speech Pacing rules above). Vary your openers — do not reuse the same phrase.");
        sb.AppendLine("- Keep the full spoken response to 1-2 sentences (under 25 words total)");
        sb.AppendLine("- Write the way a real person talks — short, punchy phrases. Do NOT chain clauses with em dashes, commas, or semicolons into one long sentence");
        sb.AppendLine("- Do NOT include any marker text in the spoken portion");

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
    /// When split prompt parts are provided, enables prompt caching so Anthropic caches the system
    /// prompt blocks — warming the cache for the first respond-to-turn call.
    /// </summary>
    public async Task<string> GeneratePrimaryQuestionAsync(
        string systemPromptStatic,
        string? systemPromptInterviewPart,
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
        var enableCaching = systemPromptInterviewPart != null;
        var response = await GatewayFacade.GenerateAnthropicCompletion(
            systemPromptStatic, history,
            enablePromptCaching: enableCaching,
            systemPromptInterviewPart: systemPromptInterviewPart).ConfigureAwait(false);
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
            sb.AppendLine("IMPORTANT: This is the MIDDLE of an ongoing interview, NOT the beginning. The candidate has already been greeted and has answered previous questions.");
            sb.AppendLine("Do NOT use opening or introductory language (e.g. \"let's kick things off\", \"let's get started\", \"let's begin\", \"welcome\", \"thanks for joining\").");
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
            sb.AppendLine("Respond with ONLY the single question to ask, nothing else. No quotes, no preamble, no introductory phrases (e.g. no \"let's kick things off\", \"let's get started\").");
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
