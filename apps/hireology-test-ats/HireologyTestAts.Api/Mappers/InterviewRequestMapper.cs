using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Mappers;

public static class InterviewRequestMapper
{
    public static InterviewRequestResource ToResource(InterviewRequest request)
    {
        return new InterviewRequestResource
        {
            Id = request.Id,
            ApplicantId = request.ApplicantId,
            JobId = request.JobId,
            OrchestratorInterviewId = request.OrchestratorInterviewId,
            InviteUrl = request.InviteUrl,
            ShortCode = request.ShortCode,
            Status = request.Status,
            Score = request.Score,
            ResultSummary = request.ResultSummary,
            ResultRecommendation = request.ResultRecommendation,
            ResultStrengths = request.ResultStrengths,
            ResultAreasForImprovement = request.ResultAreasForImprovement,
            WebhookReceivedAt = request.WebhookReceivedAt,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }

    public static IReadOnlyList<InterviewRequestResource> ToResource(IEnumerable<InterviewRequest> requests)
    {
        return requests.Select(ToResource).ToList();
    }

    public static AgentResource ToAgentResource(OrchestratorAgent agent)
    {
        return new AgentResource
        {
            Id = agent.Id,
            DisplayName = agent.DisplayName,
            ProfileImageUrl = agent.ProfileImageUrl
        };
    }

    public static IReadOnlyList<AgentResource> ToAgentResource(IEnumerable<OrchestratorAgent> agents)
    {
        return agents.Select(ToAgentResource).ToList();
    }

    public static InterviewGuideResource ToInterviewGuideResource(OrchestratorInterviewGuide guide)
    {
        return new InterviewGuideResource
        {
            Id = guide.Id,
            Name = guide.Name,
            Description = guide.Description,
            QuestionCount = guide.QuestionCount,
            IsActive = guide.IsActive
        };
    }

    public static IReadOnlyList<InterviewGuideResource> ToInterviewGuideResource(IEnumerable<OrchestratorInterviewGuide> guides)
    {
        return guides.Select(ToInterviewGuideResource).ToList();
    }

    public static InterviewConfigurationResource ToConfigurationResource(OrchestratorInterviewConfiguration config)
    {
        return new InterviewConfigurationResource
        {
            Id = config.Id,
            Name = config.Name,
            Description = config.Description,
            AgentId = config.AgentId,
            AgentDisplayName = config.AgentDisplayName,
            QuestionCount = config.QuestionCount
        };
    }

    public static IReadOnlyList<InterviewConfigurationResource> ToConfigurationResource(IEnumerable<OrchestratorInterviewConfiguration> configs)
    {
        return configs.Select(ToConfigurationResource).ToList();
    }
}
