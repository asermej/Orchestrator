using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

/// <summary>
/// Mapper class for converting between InterviewConfiguration domain objects and API models.
/// </summary>
public static class InterviewConfigurationMapper
{
    /// <summary>
    /// Maps an InterviewConfiguration domain object to an InterviewConfigurationResource for API responses.
    /// </summary>
    public static InterviewConfigurationResource ToResource(InterviewConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var questions = config.Questions?.Select(ToQuestionResource).ToList() ?? new List<InterviewConfigurationQuestionResource>();
        
        return new InterviewConfigurationResource
        {
            Id = config.Id,
            GroupId = config.GroupId,
            OrganizationId = config.OrganizationId,
            InterviewGuideId = config.InterviewGuideId,
            AgentId = config.AgentId,
            Name = config.Name,
            Description = config.Description,
            ScoringRubric = config.ScoringRubric,
            IsActive = config.IsActive,
            Questions = questions,
            QuestionCount = config.QuestionCount > 0 ? config.QuestionCount : questions.Count,
            InterviewGuide = config.InterviewGuide != null ? InterviewGuideMapper.ToResource(config.InterviewGuide) : null,
            Agent = config.Agent != null ? AgentMapper.ToResource(config.Agent) : null,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt,
            CreatedBy = config.CreatedBy,
            UpdatedBy = config.UpdatedBy
        };
    }

    /// <summary>
    /// Maps a collection of InterviewConfiguration domain objects to resources.
    /// </summary>
    public static IEnumerable<InterviewConfigurationResource> ToResource(IEnumerable<InterviewConfiguration> configs)
    {
        ArgumentNullException.ThrowIfNull(configs);

        return configs.Select(ToResource);
    }

    /// <summary>
    /// Maps an InterviewConfigurationQuestion domain object to a resource.
    /// </summary>
    public static InterviewConfigurationQuestionResource ToQuestionResource(InterviewConfigurationQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        return new InterviewConfigurationQuestionResource
        {
            Id = question.Id,
            InterviewConfigurationId = question.InterviewConfigurationId,
            Question = question.Question,
            DisplayOrder = question.DisplayOrder,
            ScoringWeight = question.ScoringWeight,
            ScoringGuidance = question.ScoringGuidance,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a CreateInterviewConfigurationResource to an InterviewConfiguration domain object.
    /// </summary>
    public static InterviewConfiguration ToDomain(CreateInterviewConfigurationResource createResource)
    {
        ArgumentNullException.ThrowIfNull(createResource);

        var config = new InterviewConfiguration
        {
            GroupId = createResource.GroupId,
            OrganizationId = createResource.OrganizationId,
            InterviewGuideId = createResource.InterviewGuideId,
            AgentId = createResource.AgentId,
            Name = createResource.Name,
            Description = createResource.Description,
            ScoringRubric = createResource.ScoringRubric,
            IsActive = createResource.IsActive,
            CreatedBy = createResource.CreatedBy
        };

        if (createResource.Questions != null)
        {
            config.Questions = createResource.Questions.Select((q, index) => new InterviewConfigurationQuestion
            {
                Question = q.Question,
                DisplayOrder = q.DisplayOrder > 0 ? q.DisplayOrder : index,
                ScoringWeight = q.ScoringWeight,
                ScoringGuidance = q.ScoringGuidance
            }).ToList();
        }

        return config;
    }

    /// <summary>
    /// Maps an UpdateInterviewConfigurationResource to an InterviewConfiguration domain object.
    /// </summary>
    public static InterviewConfiguration ToDomain(UpdateInterviewConfigurationResource updateResource, InterviewConfiguration existingConfig)
    {
        ArgumentNullException.ThrowIfNull(updateResource);
        ArgumentNullException.ThrowIfNull(existingConfig);

        return new InterviewConfiguration
        {
            Id = existingConfig.Id,
            GroupId = existingConfig.GroupId,
            OrganizationId = existingConfig.OrganizationId,
            InterviewGuideId = updateResource.InterviewGuideId ?? existingConfig.InterviewGuideId,
            AgentId = existingConfig.AgentId,
            Name = updateResource.Name ?? existingConfig.Name,
            Description = updateResource.Description ?? existingConfig.Description,
            ScoringRubric = updateResource.ScoringRubric ?? existingConfig.ScoringRubric,
            IsActive = updateResource.IsActive ?? existingConfig.IsActive,
            CreatedAt = existingConfig.CreatedAt,
            CreatedBy = existingConfig.CreatedBy,
            UpdatedBy = updateResource.UpdatedBy
        };
    }

    /// <summary>
    /// Maps a list of CreateInterviewConfigurationQuestionResource to domain objects.
    /// </summary>
    public static List<InterviewConfigurationQuestion> ToQuestionsDomain(List<CreateInterviewConfigurationQuestionResource> questions)
    {
        if (questions == null) return new List<InterviewConfigurationQuestion>();

        return questions.Select((q, index) => new InterviewConfigurationQuestion
        {
            Question = q.Question,
            DisplayOrder = q.DisplayOrder > 0 ? q.DisplayOrder : index,
            ScoringWeight = q.ScoringWeight,
            ScoringGuidance = q.ScoringGuidance
        }).ToList();
    }
}
