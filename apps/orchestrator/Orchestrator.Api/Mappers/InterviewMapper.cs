using System.Text.Json;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

public static class InterviewMapper
{
    public static InterviewResource ToResource(Interview interview)
    {
        ArgumentNullException.ThrowIfNull(interview);
        return new InterviewResource
        {
            Id = interview.Id,
            JobId = interview.JobId,
            ApplicantId = interview.ApplicantId,
            AgentId = interview.AgentId,
            InterviewConfigurationId = interview.InterviewConfigurationId,
            InterviewGuideId = interview.InterviewGuideId,
            InterviewTemplateId = interview.InterviewTemplateId,
            Token = interview.Token,
            Status = interview.Status,
            InterviewType = interview.InterviewType,
            ScheduledAt = interview.ScheduledAt,
            StartedAt = interview.StartedAt,
            CompletedAt = interview.CompletedAt,
            CurrentQuestionIndex = interview.CurrentQuestionIndex,
            CreatedAt = interview.CreatedAt,
            UpdatedAt = interview.UpdatedAt
        };
    }

    public static IEnumerable<InterviewResource> ToResource(IEnumerable<Interview> interviews)
    {
        ArgumentNullException.ThrowIfNull(interviews);
        return interviews.Select(ToResource);
    }

    public static InterviewResponseResource ToResponseResource(InterviewResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new InterviewResponseResource
        {
            Id = response.Id,
            InterviewId = response.InterviewId,
            QuestionId = response.QuestionId,
            QuestionText = response.QuestionText,
            Transcript = response.Transcript,
            AudioUrl = response.AudioUrl,
            DurationSeconds = response.DurationSeconds,
            ResponseOrder = response.ResponseOrder,
            IsFollowUp = response.IsFollowUp,
            FollowUpTemplateId = response.FollowUpTemplateId,
            QuestionType = response.QuestionType,
            AiAnalysis = response.AiAnalysis,
            CreatedAt = response.CreatedAt
        };
    }

    public static InterviewResultResource ToResultResource(InterviewResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var questionScores = new List<QuestionScoreResource>();
        if (!string.IsNullOrWhiteSpace(result.QuestionScores))
        {
            try
            {
                var scores = JsonSerializer.Deserialize<List<QuestionScore>>(result.QuestionScores, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (scores != null)
                {
                    questionScores = scores.Select(qs => new QuestionScoreResource
                    {
                        QuestionIndex = qs.QuestionIndex,
                        Question = qs.Question,
                        Score = qs.Score,
                        MaxScore = qs.MaxScore,
                        Weight = qs.Weight,
                        Feedback = qs.Feedback
                    }).ToList();
                }
            }
            catch (JsonException)
            {
                // If JSON is malformed, return empty scores
            }
        }

        return new InterviewResultResource
        {
            Id = result.Id,
            InterviewId = result.InterviewId,
            Summary = result.Summary,
            Score = result.Score,
            OverallScoreDisplay = result.OverallScoreDisplay,
            Recommendation = result.Recommendation,
            RecommendationTier = result.RecommendationTier,
            Strengths = result.Strengths,
            AreasForImprovement = result.AreasForImprovement,
            FullTranscriptUrl = result.FullTranscriptUrl,
            WebhookSentAt = result.WebhookSentAt,
            QuestionScores = questionScores,
            CreatedAt = result.CreatedAt
        };
    }

    public static Interview ToDomain(CreateInterviewResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return new Interview
        {
            JobId = resource.JobId,
            ApplicantId = resource.ApplicantId,
            AgentId = resource.AgentId,
            InterviewType = resource.InterviewType,
            ScheduledAt = resource.ScheduledAt
        };
    }

    public static InterviewResponse ToResponseDomain(CreateInterviewResponseResource resource, Guid interviewId)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return new InterviewResponse
        {
            InterviewId = interviewId,
            QuestionId = resource.QuestionId,
            QuestionText = resource.QuestionText,
            Transcript = resource.Transcript,
            AudioUrl = resource.AudioUrl,
            DurationSeconds = resource.DurationSeconds,
            ResponseOrder = resource.ResponseOrder,
            IsFollowUp = resource.IsFollowUp,
            FollowUpTemplateId = resource.FollowUpTemplateId,
            QuestionType = resource.IsFollowUp ? "followup" : "main"
        };
    }

    public static InterviewResult ToResultDomain(CreateInterviewResultResource resource, Guid interviewId)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return new InterviewResult
        {
            InterviewId = interviewId,
            Summary = resource.Summary,
            Score = resource.Score,
            Recommendation = resource.Recommendation,
            Strengths = resource.Strengths,
            AreasForImprovement = resource.AreasForImprovement
        };
    }

    public static CompetencyResponseResource ToResource(CompetencyResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new CompetencyResponseResource
        {
            Id = response.Id,
            InterviewId = response.InterviewId,
            CompetencyId = response.CompetencyId,
            CompetencyScore = response.CompetencyScore,
            CompetencyRationale = response.CompetencyRationale,
            FollowUpCount = response.FollowUpCount,
            ScoringWeight = response.ScoringWeight,
            CompetencyTranscript = response.CompetencyTranscript,
            GeneratedQuestionText = response.GeneratedQuestionText,
            QuestionsAsked = response.QuestionsAsked,
            ResponseText = response.ResponseText,
            ResponseAudioUrl = response.ResponseAudioUrl,
            CompetencySkipped = response.CompetencySkipped,
            SkipReason = response.SkipReason,
            CreatedAt = response.CreatedAt,
            UpdatedAt = response.UpdatedAt
        };
    }

    public static CompetencyResponse ToCompetencyResponseDomain(UpsertCompetencyResponseResource resource, Guid interviewId)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return new CompetencyResponse
        {
            InterviewId = interviewId,
            CompetencyId = resource.CompetencyId,
            CompetencyScore = resource.CompetencyScore,
            CompetencyRationale = resource.CompetencyRationale,
            FollowUpCount = resource.FollowUpCount,
            ScoringWeight = resource.ScoringWeight,
            QuestionsAsked = resource.QuestionsAsked,
            ResponseText = resource.ResponseText,
            ResponseAudioUrl = resource.ResponseAudioUrl
        };
    }
}
