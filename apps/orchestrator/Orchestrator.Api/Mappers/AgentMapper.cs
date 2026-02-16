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
    /// <param name="isInherited">Whether this agent is inherited from a parent organization</param>
    /// <param name="ownerOrganizationName">The name of the organization that owns this agent</param>
    /// <returns>An AgentResource object suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when agent is null</exception>
    public static AgentResource ToResource(Agent agent, bool isInherited = false, string? ownerOrganizationName = null)
    {
        ArgumentNullException.ThrowIfNull(agent);

        return new AgentResource
        {
            Id = agent.Id,
            GroupId = agent.GroupId,
            OrganizationId = agent.OrganizationId,
            VisibilityScope = agent.VisibilityScope,
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
            UpdatedAt = agent.UpdatedAt,
            IsInherited = isInherited,
            OwnerOrganizationName = ownerOrganizationName
        };
    }

    /// <summary>
    /// Maps a collection of Agent domain objects to AgentResource objects.
    /// </summary>
    /// <param name="agents">The collection of domain Agent objects to map</param>
    /// <param name="isInherited">Whether these agents are inherited from parent organizations</param>
    /// <param name="orgNameLookup">Optional lookup for organization names by ID</param>
    /// <returns>A collection of AgentResource objects suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when agents is null</exception>
    public static IEnumerable<AgentResource> ToResource(IEnumerable<Agent> agents, bool isInherited = false, IDictionary<Guid, string>? orgNameLookup = null)
    {
        ArgumentNullException.ThrowIfNull(agents);

        return agents.Select(a =>
        {
            string? ownerOrgName = null;
            if (orgNameLookup != null && a.OrganizationId.HasValue)
            {
                orgNameLookup.TryGetValue(a.OrganizationId.Value, out ownerOrgName);
            }
            return ToResource(a, isInherited, ownerOrgName);
        });
    }

    /// <summary>
    /// Maps a CreateAgentResource to an Agent domain object for creation.
    /// </summary>
    /// <param name="createResource">The CreateAgentResource from API request</param>
    /// <param name="groupId">The group ID to use (resolved by controller if not in request)</param>
    /// <returns>An Agent domain object ready for creation</returns>
    /// <exception cref="ArgumentNullException">Thrown when createResource is null</exception>
    public static Agent ToDomain(CreateAgentResource createResource, Guid groupId)
    {
        ArgumentNullException.ThrowIfNull(createResource);

        return new Agent
        {
            GroupId = groupId,
            OrganizationId = createResource.OrganizationId,
            VisibilityScope = createResource.VisibilityScope ?? AgentVisibilityScope.OrganizationOnly,
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
            GroupId = existingAgent.GroupId,
            OrganizationId = existingAgent.OrganizationId,
            VisibilityScope = updateResource.VisibilityScope ?? existingAgent.VisibilityScope,
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
