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
    public async Task<PaginatedResult<Interview>> SearchInterviews(Guid? jobId, Guid? applicantId, Guid? agentId, string? status, int pageNumber, int pageSize)
    {
        return await InterviewManager.SearchInterviews(jobId, applicantId, agentId, status, pageNumber, pageSize).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an Interview
    /// </summary>
    public async Task<Interview> UpdateInterview(Interview interview)
    {
        return await InterviewManager.UpdateInterview(interview).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts an Interview
    /// </summary>
    public async Task<Interview> StartInterview(Guid interviewId)
    {
        return await InterviewManager.StartInterview(interviewId).ConfigureAwait(false);
    }

    /// <summary>
    /// Completes an Interview
    /// </summary>
    public async Task<Interview> CompleteInterview(Guid interviewId)
    {
        var interview = await InterviewManager.CompleteInterview(interviewId).ConfigureAwait(false);
        
        // Send webhook notification
        await SendInterviewCompletedWebhookAsync(interview);
        
        return interview;
    }

    /// <summary>
    /// Sends a webhook notification when an interview is completed
    /// </summary>
    private async Task SendInterviewCompletedWebhookAsync(Interview interview)
    {
        try
        {
            // Get the job to find the organization ID
            var job = await JobManager.GetJobById(interview.JobId).ConfigureAwait(false);
            if (job == null) return;

            // Get the interview result if available
            var result = await InterviewManager.GetResultByInterviewId(interview.Id).ConfigureAwait(false);

            // Send the webhook
            await WebhookManager.SendInterviewWebhookAsync(
                job.OrganizationId,
                WebhookEventTypes.InterviewCompleted,
                interview,
                result
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log but don't fail the interview completion
            System.Diagnostics.Debug.WriteLine($"Failed to send webhook for interview {interview.Id}: {ex.Message}");
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

    // Test Interview Methods

    /// <summary>
    /// Creates a test interview from an interview configuration
    /// </summary>
    public async Task<Interview> CreateTestInterview(Guid interviewConfigurationId, string? testUserName = null)
    {
        // Get the configuration with questions
        var config = await InterviewConfigurationManager.GetConfigurationByIdWithQuestions(interviewConfigurationId).ConfigureAwait(false);
        if (config == null)
        {
            throw new InterviewConfigurationNotFoundException($"Interview configuration with ID {interviewConfigurationId} not found");
        }

        // Create a test applicant (or use a placeholder)
        var testApplicantId = Guid.NewGuid().ToString("N");
        var testApplicant = new Applicant
        {
            OrganizationId = config.OrganizationId,
            ExternalApplicantId = $"test-{testApplicantId}",
            FirstName = testUserName ?? "Test",
            LastName = "User",
            Email = $"test-{testApplicantId}@test.local"
        };
        var createdApplicant = await ApplicantManager.CreateApplicant(testApplicant).ConfigureAwait(false);

        // Create a test job (or use a placeholder)
        var testJobId = Guid.NewGuid().ToString("N");
        var testJob = new Job
        {
            OrganizationId = config.OrganizationId,
            ExternalJobId = $"test-job-{testJobId}",
            Title = $"Test Interview - {config.Name}",
            Status = "active"
        };
        var createdJob = await JobManager.CreateJob(testJob).ConfigureAwait(false);

        // Create the interview
        var interview = new Interview
        {
            JobId = createdJob.Id,
            ApplicantId = createdApplicant.Id,
            AgentId = config.AgentId,
            Status = InterviewStatus.Pending,
            InterviewType = InterviewType.Voice,
            Token = Guid.NewGuid().ToString("N")
        };

        return await InterviewManager.CreateInterview(interview).ConfigureAwait(false);
    }

    /// <summary>
    /// Scores a completed test interview using AI
    /// </summary>
    public async Task<InterviewResult> ScoreTestInterview(Guid interviewId, Guid interviewConfigurationId)
    {
        // Get the interview
        var interview = await InterviewManager.GetInterviewById(interviewId).ConfigureAwait(false);
        if (interview == null)
        {
            throw new InterviewNotFoundException($"Interview with ID {interviewId} not found");
        }

        // Get the configuration with questions
        var config = await InterviewConfigurationManager.GetConfigurationByIdWithQuestions(interviewConfigurationId).ConfigureAwait(false);
        if (config == null)
        {
            throw new InterviewConfigurationNotFoundException($"Interview configuration with ID {interviewConfigurationId} not found");
        }

        // Get the responses
        var responses = await InterviewManager.GetResponsesByInterviewId(interviewId).ConfigureAwait(false);

        // Calculate scores based on responses and configuration
        var questionScores = new List<QuestionScore>();
        decimal totalWeightedScore = 0;
        decimal totalWeight = 0;

        foreach (var question in config.Questions.OrderBy(q => q.DisplayOrder))
        {
            var response = responses.FirstOrDefault(r => r.ResponseOrder == question.DisplayOrder);
            
            // Simple scoring based on response length and content
            // In a real implementation, this would use AI to evaluate the response
            decimal score = 0;
            string feedback = "No response provided";

            if (response != null && !string.IsNullOrWhiteSpace(response.Transcript))
            {
                // Basic scoring heuristic - can be replaced with AI scoring
                var responseLength = response.Transcript.Length;
                if (responseLength > 200)
                {
                    score = 8 + (Math.Min(responseLength - 200, 300) / 100m);
                }
                else if (responseLength > 100)
                {
                    score = 6 + ((responseLength - 100) / 50m);
                }
                else if (responseLength > 50)
                {
                    score = 4 + ((responseLength - 50) / 25m);
                }
                else
                {
                    score = Math.Max(1, responseLength / 12.5m);
                }

                score = Math.Min(10, Math.Max(0, score));
                feedback = $"Response received with {responseLength} characters";
            }

            questionScores.Add(new QuestionScore
            {
                QuestionIndex = question.DisplayOrder,
                Question = question.Question,
                Score = score,
                MaxScore = 10,
                Weight = question.ScoringWeight,
                Feedback = feedback
            });

            totalWeightedScore += score * question.ScoringWeight;
            totalWeight += question.ScoringWeight;
        }

        var overallScore = totalWeight > 0 ? totalWeightedScore / totalWeight : 0;

        // Create the result
        var result = new InterviewResult
        {
            InterviewId = interviewId,
            Score = (int)Math.Round(overallScore * 10), // Convert to 0-100 scale
            Summary = GenerateInterviewSummary(overallScore, questionScores),
            Recommendation = GenerateRecommendation(overallScore),
            Strengths = string.Join("; ", questionScores.Where(q => q.Score >= 7).Select(q => q.Question.Substring(0, Math.Min(50, q.Question.Length)))),
            AreasForImprovement = string.Join("; ", questionScores.Where(q => q.Score < 5).Select(q => q.Question.Substring(0, Math.Min(50, q.Question.Length))))
        };

        return await InterviewManager.CreateResult(result).ConfigureAwait(false);
    }

    private string GenerateInterviewSummary(decimal overallScore, List<QuestionScore> questionScores)
    {
        var strongAreas = questionScores.Where(q => q.Score >= 7).Select(q => q.Question).Take(3).ToList();
        var weakAreas = questionScores.Where(q => q.Score < 5).Select(q => q.Question).Take(3).ToList();

        var summary = $"The candidate achieved an overall score of {overallScore:F1}/10. ";
        
        if (strongAreas.Any())
        {
            summary += $"Strong performance was noted in: {string.Join(", ", strongAreas.Select(q => $"\"{q.Substring(0, Math.Min(50, q.Length))}...\""))}. ";
        }
        
        if (weakAreas.Any())
        {
            summary += $"Areas for improvement include: {string.Join(", ", weakAreas.Select(q => $"\"{q.Substring(0, Math.Min(50, q.Length))}...\""))}. ";
        }

        return summary;
    }

    private string GenerateRecommendation(decimal overallScore)
    {
        if (overallScore >= 8)
        {
            return "Strong Recommend - Excellent overall";
        }
        else if (overallScore >= 6)
        {
            return "Recommend - Good with minor gaps";
        }
        else if (overallScore >= 4)
        {
            return "Consider - Needs more evaluation";
        }
        else
        {
            return "Do Not Recommend - Below expectations";
        }
    }
}
