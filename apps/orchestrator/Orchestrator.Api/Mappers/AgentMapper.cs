using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

/// <summary>
/// Mapper class for converting between Agent domain objects and AgentResource API models.
/// </summary>
public static class AgentMapper
{
    /// <summary>
    /// Maps an Agent domain object to an AgentResource for API responses.
    /// </summary>
    /// <param name="agent">The domain Agent object to map</param>
    /// <returns>An AgentResource object suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when agent is null</exception>
    public static AgentResource ToResource(Agent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        return new AgentResource
        {
            Id = agent.Id,
            OrganizationId = agent.OrganizationId,
            DisplayName = agent.DisplayName,
            ProfileImageUrl = agent.ProfileImageUrl,
            SystemPrompt = agent.SystemPrompt,
            InterviewGuidelines = agent.InterviewGuidelines,
            ElevenlabsVoiceId = agent.ElevenlabsVoiceId,
            VoiceStability = agent.VoiceStability,
            VoiceSimilarityBoost = agent.VoiceSimilarityBoost,
            VoiceProvider = agent.VoiceProvider,
            VoiceType = agent.VoiceType,
            VoiceName = agent.VoiceName,
            CreatedAt = agent.CreatedAt,
            UpdatedAt = agent.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a collection of Agent domain objects to AgentResource objects.
    /// </summary>
    /// <param name="agents">The collection of domain Agent objects to map</param>
    /// <returns>A collection of AgentResource objects suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when agents is null</exception>
    public static IEnumerable<AgentResource> ToResource(IEnumerable<Agent> agents)
    {
        ArgumentNullException.ThrowIfNull(agents);

        return agents.Select(ToResource);
    }

    /// <summary>
    /// Maps a CreateAgentResource to an Agent domain object for creation.
    /// </summary>
    /// <param name="createResource">The CreateAgentResource from API request</param>
    /// <param name="organizationId">The organization ID to use (resolved by controller if not in request)</param>
    /// <returns>An Agent domain object ready for creation</returns>
    /// <exception cref="ArgumentNullException">Thrown when createResource is null</exception>
    public static Agent ToDomain(CreateAgentResource createResource, Guid organizationId)
    {
        ArgumentNullException.ThrowIfNull(createResource);

        return new Agent
        {
            OrganizationId = organizationId,
            DisplayName = createResource.DisplayName,
            ProfileImageUrl = createResource.ProfileImageUrl,
            SystemPrompt = createResource.SystemPrompt,
            InterviewGuidelines = createResource.InterviewGuidelines,
            ElevenlabsVoiceId = createResource.ElevenlabsVoiceId,
            VoiceStability = createResource.VoiceStability,
            VoiceSimilarityBoost = createResource.VoiceSimilarityBoost,
            VoiceProvider = createResource.VoiceProvider,
            VoiceType = createResource.VoiceType,
            VoiceName = createResource.VoiceName
        };
    }

    /// <summary>
    /// Maps an UpdateAgentResource to an Agent domain object for updates.
    /// </summary>
    /// <param name="updateResource">The UpdateAgentResource from API request</param>
    /// <param name="existingAgent">The existing Agent domain object to update</param>
    /// <returns>An Agent domain object with updated values</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateResource or existingAgent is null</exception>
    public static Agent ToDomain(UpdateAgentResource updateResource, Agent existingAgent)
    {
        ArgumentNullException.ThrowIfNull(updateResource);
        ArgumentNullException.ThrowIfNull(existingAgent);

        return new Agent
        {
            Id = existingAgent.Id,
            OrganizationId = existingAgent.OrganizationId,
            DisplayName = updateResource.DisplayName ?? existingAgent.DisplayName,
            ProfileImageUrl = updateResource.ProfileImageUrl ?? existingAgent.ProfileImageUrl,
            SystemPrompt = updateResource.SystemPrompt ?? existingAgent.SystemPrompt,
            InterviewGuidelines = updateResource.InterviewGuidelines ?? existingAgent.InterviewGuidelines,
            ElevenlabsVoiceId = updateResource.ElevenlabsVoiceId ?? existingAgent.ElevenlabsVoiceId,
            VoiceStability = updateResource.VoiceStability ?? existingAgent.VoiceStability,
            VoiceSimilarityBoost = updateResource.VoiceSimilarityBoost ?? existingAgent.VoiceSimilarityBoost,
            VoiceProvider = updateResource.VoiceProvider ?? existingAgent.VoiceProvider,
            VoiceType = updateResource.VoiceType ?? existingAgent.VoiceType,
            VoiceName = updateResource.VoiceName ?? existingAgent.VoiceName,
            CreatedAt = existingAgent.CreatedAt,
            UpdatedAt = existingAgent.UpdatedAt,
            CreatedBy = existingAgent.CreatedBy,
            UpdatedBy = existingAgent.UpdatedBy,
            IsDeleted = existingAgent.IsDeleted,
            DeletedAt = existingAgent.DeletedAt,
            DeletedBy = existingAgent.DeletedBy
        };
    }
}
