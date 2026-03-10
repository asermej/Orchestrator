using System.Text.Json;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    private ResponseClassifier? _responseClassifier;
    private ResponseClassifier ResponseClassifier =>
        _responseClassifier ??= new ResponseClassifier(_serviceLocator);

    /// <summary>
    /// Classifies a candidate's response before STAR evaluation.
    /// Returns a classification result indicating how the response should be handled.
    /// This is completely separate from the STAR evaluation prompt.
    /// </summary>
    public async Task<ResponseClassificationResult> ClassifyResponseAsync(
        string systemPrompt,
        string candidateResponse,
        string currentQuestion,
        string competencyName,
        CancellationToken cancellationToken = default)
    {
        return await ResponseClassifier.ClassifyResponseAsync(
            systemPrompt,
            candidateResponse,
            currentQuestion,
            competencyName,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Classifies a candidate's response and, if on_topic, immediately evaluates it in a single round-trip.
    /// Eliminates the sequential classify-then-evaluate pattern for faster turn processing.
    /// </summary>
    public async Task<ClassifyAndEvaluateResult> ClassifyAndEvaluateResponseAsync(
        string systemPrompt,
        Competency competency,
        string candidateResponse,
        string currentQuestion,
        string competencyTranscript,
        string roleName,
        string industry,
        string? previousFollowUpTarget = null,
        CancellationToken cancellationToken = default)
    {
        var classification = await ResponseClassifier.ClassifyResponseAsync(
            systemPrompt,
            candidateResponse,
            currentQuestion,
            competency.Name,
            cancellationToken
        ).ConfigureAwait(false);

        if (classification.Classification != "on_topic")
        {
            return new ClassifyAndEvaluateResult
            {
                Classification = classification,
                Evaluation = null
            };
        }

        var evaluation = await InterviewRuntimeManager.EvaluateCompetencyResponseAsync(
            systemPrompt,
            competency,
            competencyTranscript,
            roleName,
            industry,
            previousFollowUpTarget,
            cancellationToken
        ).ConfigureAwait(false);

        return new ClassifyAndEvaluateResult
        {
            Classification = classification,
            Evaluation = evaluation
        };
    }

    /// <summary>
    /// Records a skipped competency (e.g., candidate gave two off-topic responses).
    /// Sets competency_skipped = true with the skip reason. Does NOT run STAR evaluation.
    /// </summary>
    public async Task<CompetencyResponse> ScoreAndRecordSkippedCompetencyAsync(
        Guid interviewId,
        Competency competency,
        string primaryQuestion,
        string skipReason)
    {
        var competencyResponse = new CompetencyResponse
        {
            InterviewId = interviewId,
            CompetencyId = competency.Id,
            CompetencyScore = 0,
            CompetencyRationale = null,
            FollowUpCount = 0,
            QuestionsAsked = JsonSerializer.Serialize(new List<string> { primaryQuestion }),
            GeneratedQuestionText = primaryQuestion,
            ResponseText = null,
            CompetencyTranscript = null,
            ScoringWeight = competency.DefaultWeight,
            CompetencySkipped = true,
            SkipReason = skipReason
        };

        return await UpsertCompetencyResponse(competencyResponse).ConfigureAwait(false);
    }
}
