namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Creates a new Interview
    /// </summary>
    public async Task<Interview> CreateInterview(Interview interview)
    {
        return await InterviewManager.CreateInterview(interview).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an Interview by ID
    /// </summary>
    public async Task<Interview?> GetInterviewById(Guid id)
    {
        return await InterviewManager.GetInterviewById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an Interview by token (for public access)
    /// </summary>
    public async Task<Interview?> GetInterviewByToken(string token)
    {
        return await InterviewManager.GetInterviewByToken(token).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for Interviews
    /// </summary>
    public async Task<PaginatedResult<Interview>> SearchInterviews(Guid? groupId, Guid? jobId, Guid? applicantId, Guid? agentId, string? status, int pageNumber, int pageSize)
    {
        return await InterviewManager.SearchInterviews(groupId, jobId, applicantId, agentId, status, pageNumber, pageSize).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an Interview
    /// </summary>
    public async Task<Interview> UpdateInterview(Interview interview)
    {
        return await InterviewManager.UpdateInterview(interview).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts an Interview and sends a webhook notification
    /// </summary>
    public async Task<Interview> StartInterview(Guid interviewId)
    {
        var interview = await InterviewManager.StartInterview(interviewId).ConfigureAwait(false);
        await SendInterviewWebhookAsync(interview, WebhookEventTypes.InterviewStarted);
        return interview;
    }

    /// <summary>
    /// Completes an Interview
    /// </summary>
    public async Task<Interview> CompleteInterview(Guid interviewId)
    {
        var interview = await InterviewManager.CompleteInterview(interviewId).ConfigureAwait(false);

        // Auto-score using competency responses if template-based
        if (interview.InterviewTemplateId.HasValue)
        {
            try
            {
                await ScoreInterviewFromCompetencyResponses(interview.Id).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-scoring failed for interview {interview.Id}: {ex.Message}");
            }
        }

        await SendInterviewWebhookAsync(interview, WebhookEventTypes.InterviewCompleted);
        return interview;
    }

    /// <summary>
    /// Sends a webhook notification for any interview status change.
    /// Fetches the job (for group routing) and result (if available) automatically.
    /// </summary>
    private async Task SendInterviewWebhookAsync(Interview interview, string eventType)
    {
        try
        {
            var job = await JobManager.GetJobById(interview.JobId).ConfigureAwait(false);
            if (job == null) return;

            var result = await InterviewManager.GetResultByInterviewId(interview.Id).ConfigureAwait(false);

            await WebhookManager.SendInterviewWebhookAsync(
                job.GroupId,
                eventType,
                interview,
                result
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to send {eventType} webhook for interview {interview.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes an Interview
    /// </summary>
    public async Task<bool> DeleteInterview(Guid id)
    {
        return await InterviewManager.DeleteInterview(id).ConfigureAwait(false);
    }

    // Interview Responses

    /// <summary>
    /// Adds a response to an Interview
    /// </summary>
    public async Task<InterviewResponse> AddInterviewResponse(InterviewResponse response)
    {
        return await InterviewManager.AddResponse(response).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an InterviewResponse by ID
    /// </summary>
    public async Task<InterviewResponse?> GetInterviewResponseById(Guid id)
    {
        return await InterviewManager.GetResponseById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all responses for an Interview
    /// </summary>
    public async Task<IEnumerable<InterviewResponse>> GetInterviewResponsesByInterviewId(Guid interviewId)
    {
        return await InterviewManager.GetResponsesByInterviewId(interviewId).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an InterviewResponse
    /// </summary>
    public async Task<InterviewResponse> UpdateInterviewResponse(InterviewResponse response)
    {
        return await InterviewManager.UpdateResponse(response).ConfigureAwait(false);
    }

    // Interview Results

    /// <summary>
    /// Creates an InterviewResult
    /// </summary>
    public async Task<InterviewResult> CreateInterviewResult(InterviewResult result)
    {
        return await InterviewManager.CreateResult(result).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an InterviewResult by ID
    /// </summary>
    public async Task<InterviewResult?> GetInterviewResultById(Guid id)
    {
        return await InterviewManager.GetResultById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an InterviewResult by Interview ID
    /// </summary>
    public async Task<InterviewResult?> GetInterviewResultByInterviewId(Guid interviewId)
    {
        return await InterviewManager.GetResultByInterviewId(interviewId).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an InterviewResult
    /// </summary>
    public async Task<InterviewResult> UpdateInterviewResult(InterviewResult result)
    {
        return await InterviewManager.UpdateResult(result).ConfigureAwait(false);
    }

    // Competency responses (per-competency holistic scores for AI interviews)

    /// <summary>
    /// Gets all competency responses for an interview.
    /// </summary>
    public async Task<List<CompetencyResponse>> GetCompetencyResponsesByInterviewId(Guid interviewId)
    {
        return await InterviewManager.GetCompetencyResponsesByInterviewId(interviewId).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates or updates a competency response for an interview (upsert by interview_id + competency_id).
    /// </summary>
    public async Task<CompetencyResponse> UpsertCompetencyResponse(CompetencyResponse response)
    {
        return await InterviewManager.UpsertCompetencyResponse(response).ConfigureAwait(false);
    }

    // Test Interview Methods

    /// <summary>
    /// Creates a test interview from an interview template
    /// </summary>
    public async Task<Interview> CreateTestInterviewFromTemplate(Guid interviewTemplateId, string? testUserName = null)
    {
        var template = await InterviewTemplateManager.GetTemplateById(interviewTemplateId).ConfigureAwait(false);
        if (template == null)
        {
            throw new InterviewTemplateNotFoundException($"Interview template with ID {interviewTemplateId} not found");
        }

        var testApplicantId = Guid.NewGuid().ToString("N");
        var testApplicant = new Applicant
        {
            GroupId = template.GroupId,
            ExternalApplicantId = $"test-{testApplicantId}",
            FirstName = testUserName ?? "Test",
            LastName = "User",
            Email = $"test-{testApplicantId}@test.local"
        };
        var createdApplicant = await ApplicantManager.CreateApplicant(testApplicant).ConfigureAwait(false);

        var testJobId = Guid.NewGuid().ToString("N");
        var testJob = new Job
        {
            GroupId = template.GroupId,
            ExternalJobId = $"test-job-{testJobId}",
            Title = $"Test Interview - {template.Name}",
            Status = "active"
        };
        var createdJob = await JobManager.CreateJob(testJob).ConfigureAwait(false);

        var interview = new Interview
        {
            JobId = createdJob.Id,
            ApplicantId = createdApplicant.Id,
            AgentId = template.AgentId ?? Guid.Empty,
            InterviewTemplateId = interviewTemplateId,
            Status = InterviewStatus.Pending,
            InterviewType = InterviewType.Voice,
            Token = Guid.NewGuid().ToString("N")
        };

        return await InterviewManager.CreateInterview(interview).ConfigureAwait(false);
    }

    /// <summary>
    /// Scores a completed interview using competency responses (weighted average).
    /// Computes overall_score_display (0-100) and recommendation_tier from global thresholds.
    /// </summary>
    public async Task<InterviewResult> ScoreInterviewFromCompetencyResponses(Guid interviewId)
    {
        var interview = await InterviewManager.GetInterviewById(interviewId).ConfigureAwait(false);
        if (interview == null)
        {
            throw new InterviewNotFoundException($"Interview with ID {interviewId} not found");
        }

        var competencyResponses = await InterviewManager.GetCompetencyResponsesByInterviewId(interviewId).ConfigureAwait(false);
        if (competencyResponses.Count == 0)
        {
            throw new InvalidOperationException("No competency responses found for this interview.");
        }

        var scoredResponses = competencyResponses.Where(cr => !cr.CompetencySkipped).ToList();
        var skippedCount = competencyResponses.Count - scoredResponses.Count;

        decimal weightedSum = 0;
        decimal totalWeight = 0;
        foreach (var cr in scoredResponses)
        {
            var weight = cr.ScoringWeight ?? 0;
            weightedSum += cr.CompetencyScore * weight;
            totalWeight += weight;
        }

        var weightedAverage = totalWeight > 0 ? weightedSum / totalWeight : 0;
        var scoreRaw = (int)Math.Round(weightedAverage * 100);
        var displayScore = (int)Math.Round((weightedAverage / 5.0m) * 100);

        var thresholds = await InterviewManager.GetRecommendationThresholds().ConfigureAwait(false);
        var recommendationTier = DeriveRecommendationTier(displayScore, thresholds);

        var summary = skippedCount > 0
            ? $"Weighted score across {scoredResponses.Count} of {competencyResponses.Count} competencies: {displayScore} / 100 ({skippedCount} competenc{(skippedCount == 1 ? "y" : "ies")} skipped)"
            : $"Weighted score across {competencyResponses.Count} competencies: {displayScore} / 100";

        var existingResult = await InterviewManager.GetResultByInterviewId(interviewId).ConfigureAwait(false);
        if (existingResult != null)
        {
            existingResult.Score = scoreRaw;
            existingResult.OverallScoreDisplay = displayScore;
            existingResult.Summary = summary;
            existingResult.Recommendation = recommendationTier;
            existingResult.RecommendationTier = recommendationTier;
            return await InterviewManager.UpdateResult(existingResult).ConfigureAwait(false);
        }

        var result = new InterviewResult
        {
            InterviewId = interviewId,
            Score = scoreRaw,
            OverallScoreDisplay = displayScore,
            Summary = summary,
            Recommendation = recommendationTier,
            RecommendationTier = recommendationTier
        };

        return await InterviewManager.CreateResult(result).ConfigureAwait(false);
    }

    /// <summary>
    /// Derives the recommendation tier from a 0-100 display score and the global thresholds.
    /// </summary>
    private static string DeriveRecommendationTier(int displayScore, RecommendationThresholdDefaults thresholds)
    {
        if (displayScore >= thresholds.StronglyRecommendMin)
            return RecommendationTiers.StronglyRecommend;
        if (displayScore >= thresholds.RecommendMin)
            return RecommendationTiers.Recommend;
        if (displayScore >= thresholds.ConsiderMin)
            return RecommendationTiers.Consider;
        return RecommendationTiers.DoNotRecommend;
    }

    /// <summary>
    /// Gets the global recommendation thresholds.
    /// </summary>
    public async Task<RecommendationThresholdDefaults> GetRecommendationThresholds()
    {
        return await InterviewManager.GetRecommendationThresholds().ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the global recommendation thresholds.
    /// </summary>
    public async Task<RecommendationThresholdDefaults> UpdateRecommendationThresholds(RecommendationThresholdDefaults thresholds)
    {
        return await InterviewManager.UpdateRecommendationThresholds(thresholds).ConfigureAwait(false);
    }
}
