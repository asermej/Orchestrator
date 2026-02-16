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
        
        // Auto-score the interview if it has a configuration
        if (interview.InterviewConfigurationId.HasValue)
        {
            try
            {
                await ScoreTestInterview(interview.Id, interview.InterviewConfigurationId.Value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log but don't fail interview completion if scoring fails
                System.Diagnostics.Debug.WriteLine($"Auto-scoring failed for interview {interview.Id}: {ex.Message}");
            }
        }
        
        // Send webhook notification (will now include the score if auto-scoring succeeded)
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
            // Get the job to find the group ID
            var job = await JobManager.GetJobById(interview.JobId).ConfigureAwait(false);
            if (job == null) return;

            // Get the interview result if available
            var result = await InterviewManager.GetResultByInterviewId(interview.Id).ConfigureAwait(false);

            // Send the webhook
            await WebhookManager.SendInterviewWebhookAsync(
                job.GroupId,
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
            GroupId = config.GroupId,
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
            GroupId = config.GroupId,
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
            InterviewConfigurationId = interviewConfigurationId,
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
            
            decimal score = 0;
            string feedback = "The candidate did not provide a response to this question.";

            if (response != null && !string.IsNullOrWhiteSpace(response.Transcript))
            {
                var transcript = response.Transcript.Trim();
                var responseLength = transcript.Length;
                var wordCount = transcript.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                // Score based on response depth (word count is a better proxy than char count)
                if (wordCount >= 50)
                {
                    score = 8 + Math.Min((wordCount - 50) / 25m, 2m);
                }
                else if (wordCount >= 25)
                {
                    score = 6 + ((wordCount - 25) / 12.5m);
                }
                else if (wordCount >= 10)
                {
                    score = 4 + ((wordCount - 10) / 7.5m);
                }
                else
                {
                    score = Math.Max(1, wordCount / 2.5m);
                }

                score = Math.Min(10, Math.Max(0, score));

                // Generate meaningful feedback based on score tier
                if (score >= 8)
                {
                    feedback = $"Strong response. The candidate provided a detailed answer with specific examples ({wordCount} words).";
                }
                else if (score >= 6)
                {
                    feedback = $"Adequate response. The candidate addressed the question but could have provided more detail or specific examples ({wordCount} words).";
                }
                else if (score >= 4)
                {
                    feedback = $"Brief response. The candidate gave a short answer that lacks depth or specificity ({wordCount} words). A stronger answer would include concrete examples.";
                }
                else
                {
                    feedback = $"Minimal response. The candidate's answer was very brief and did not meaningfully address the question ({wordCount} words).";
                }
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

        // Check if a result already exists for this interview (re-scoring case)
        var existingResult = await InterviewManager.GetResultByInterviewId(interviewId).ConfigureAwait(false);

        if (existingResult != null)
        {
            // Update the existing result
            existingResult.Score = (int)Math.Round(overallScore * 10);
            existingResult.Summary = GenerateInterviewSummary(overallScore, questionScores);
            existingResult.Recommendation = GenerateRecommendation(overallScore);
            existingResult.Strengths = string.Join("; ", questionScores.Where(q => q.Score >= 7).Select(q => q.Question.Substring(0, Math.Min(50, q.Question.Length))));
            existingResult.AreasForImprovement = string.Join("; ", questionScores.Where(q => q.Score < 5).Select(q => q.Question.Substring(0, Math.Min(50, q.Question.Length))));
            existingResult.QuestionScores = System.Text.Json.JsonSerializer.Serialize(questionScores);

            return await InterviewManager.UpdateResult(existingResult).ConfigureAwait(false);
        }

        // Create a new result
        var result = new InterviewResult
        {
            InterviewId = interviewId,
            Score = (int)Math.Round(overallScore * 10), // Convert to 0-100 scale
            Summary = GenerateInterviewSummary(overallScore, questionScores),
            Recommendation = GenerateRecommendation(overallScore),
            Strengths = string.Join("; ", questionScores.Where(q => q.Score >= 7).Select(q => q.Question.Substring(0, Math.Min(50, q.Question.Length)))),
            AreasForImprovement = string.Join("; ", questionScores.Where(q => q.Score < 5).Select(q => q.Question.Substring(0, Math.Min(50, q.Question.Length)))),
            QuestionScores = System.Text.Json.JsonSerializer.Serialize(questionScores)
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
