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

        return new InterviewConfigurationResource
        {
            Id = config.Id,
            GroupId = config.GroupId,
            OrganizationId = config.OrganizationId,
            InterviewGuideId = config.InterviewGuideId,
            AgentId = config.AgentId,
            Name = config.Name,
            Description = config.Description,
            IsActive = config.IsActive,
            QuestionCount = config.QuestionCount,
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
    /// Maps a CreateInterviewConfigurationResource to an InterviewConfiguration domain object.
    /// </summary>
    public static InterviewConfiguration ToDomain(CreateInterviewConfigurationResource createResource, Guid groupId)
    {
        ArgumentNullException.ThrowIfNull(createResource);

        return new InterviewConfiguration
        {
            GroupId = groupId,
            OrganizationId = createResource.OrganizationId,
            InterviewGuideId = createResource.InterviewGuideId,
            AgentId = createResource.AgentId,
            Name = createResource.Name,
            Description = createResource.Description,
            IsActive = createResource.IsActive,
            CreatedBy = createResource.CreatedBy
        };
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
            IsActive = updateResource.IsActive ?? existingConfig.IsActive,
            CreatedAt = existingConfig.CreatedAt,
            CreatedBy = existingConfig.CreatedBy,
            UpdatedBy = updateResource.UpdatedBy
        };
    }
}
