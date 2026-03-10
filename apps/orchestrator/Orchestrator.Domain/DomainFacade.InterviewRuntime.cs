namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    private InterviewRuntimeManager? _interviewRuntimeManager;
    private InterviewRuntimeManager InterviewRuntimeManager =>
        _interviewRuntimeManager ??= new InterviewRuntimeManager(_serviceLocator);

    /// <summary>
    /// Loads the full runtime context for an interview (template, role, competencies, agent, applicant, job).
    /// Returns null if the interview has no template assigned.
    /// </summary>
    public async Task<InterviewRuntimeContext?> LoadInterviewRuntimeContextAsync(Guid interviewId)
    {
        var interview = await InterviewManager.GetInterviewById(interviewId).ConfigureAwait(false);
        if (interview == null)
            throw new InterviewNotFoundException($"Interview with ID {interviewId} not found");

        return await InterviewRuntimeManager.LoadInterviewContextAsync(interview).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the interview system prompt for a given runtime context.
    /// </summary>
    public string BuildInterviewSystemPrompt(InterviewRuntimeContext context)
    {
        if (context.Agent == null)
            throw new InvalidOperationException("Cannot build system prompt: no agent assigned to this interview template.");

        return InterviewRuntimeManager.BuildInterviewSystemPrompt(
            context.Agent,
            context.Template,
            context.Role,
            context.ApplicantName,
            context.JobTitle
        );
    }

    /// <summary>
    /// Resolves the opening template text with actual values.
    /// </summary>
    public string ResolveOpeningTemplate(InterviewRuntimeContext context)
    {
        var template = context.Template.OpeningTemplate ?? InterviewTemplate.DefaultOpeningTemplate;
        return InterviewRuntimeManager.ResolveTemplateVariables(
            template,
            context.ApplicantName,
            context.Agent?.DisplayName ?? "the interviewer",
            context.JobTitle
        );
    }

    /// <summary>
    /// Resolves the closing template text with actual values.
    /// </summary>
    public string ResolveClosingTemplate(InterviewRuntimeContext context)
    {
        var template = context.Template.ClosingTemplate ?? InterviewTemplate.DefaultClosingTemplate;
        return InterviewRuntimeManager.ResolveTemplateVariables(
            template,
            context.ApplicantName,
            context.Agent?.DisplayName ?? "the interviewer",
            context.JobTitle
        );
    }

    /// <summary>
    /// Evaluates a candidate's full accumulated transcript for a competency.
    /// Returns a score, rationale, and transient follow-up decision fields.
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
        return await InterviewRuntimeManager.EvaluateCompetencyResponseAsync(
            systemPrompt,
            competency,
            competencyTranscript,
            roleName,
            industry,
            previousFollowUpTarget,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates the primary interview question for a competency from the canonical example and context.
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
        return await InterviewRuntimeManager.GeneratePrimaryQuestionAsync(
            systemPrompt, competency, roleName, industry, jobTitle, applicantName, candidateContext, includeTransition, previousCompetencyName, cancellationToken
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Evaluates a candidate's full accumulated transcript with API-compatible signature.
    /// The transcript should already contain all prior exchanges concatenated.
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
        return await InterviewRuntimeManager.EvaluateCompetencyResponseAsync(
            systemPrompt,
            competency,
            competencyTranscript,
            roleName,
            industry,
            previousFollowUpTarget,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Scores a completed competency from pre-collected conversation data and records the result.
    /// Builds competency_transcript from all candidate turns concatenated in order.
    /// </summary>
    public async Task<CompetencyResponse> ScoreAndRecordCompetencyAsync(
        Guid interviewId,
        Competency competency,
        string primaryQuestion,
        string candidateResponse,
        List<FollowUpExchange>? followUpExchanges,
        HolisticEvaluationResult evaluation)
    {
        var competencyResponse = InterviewRuntimeManager.ScoreAndRecordCompetency(
            competency,
            interviewId,
            primaryQuestion,
            candidateResponse,
            followUpExchanges,
            evaluation
        );

        return await UpsertCompetencyResponse(competencyResponse).ConfigureAwait(false);
    }

}
