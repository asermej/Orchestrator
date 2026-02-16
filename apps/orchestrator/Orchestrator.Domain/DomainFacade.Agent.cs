using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    public async Task<Agent> CreateAgent(Agent agent)
    {
        return await AgentManager.CreateAgent(agent).ConfigureAwait(false);
    }

    public async Task<Agent?> GetAgentById(Guid id)
    {
        return await AgentManager.GetAgentById(id).ConfigureAwait(false);
    }

    public async Task<PaginatedResult<Agent>> SearchAgents(Guid? groupId, string? displayName, string? createdBy, string? sortBy, int pageNumber, int pageSize, IReadOnlyList<Guid>? organizationIds = null)
    {
        return await AgentManager.SearchAgents(groupId, displayName, createdBy, sortBy, pageNumber, pageSize, organizationIds).ConfigureAwait(false);
    }

    public async Task<PaginatedResult<Agent>> SearchLocalAgents(Guid groupId, Guid organizationId, string? displayName, string? sortBy, int pageNumber, int pageSize)
    {
        return await AgentManager.SearchLocalAgents(groupId, organizationId, displayName, sortBy, pageNumber, pageSize).ConfigureAwait(false);
    }

    public async Task<PaginatedResult<Agent>> SearchInheritedAgents(Guid groupId, IReadOnlyList<Guid> ancestorOrgIds, string? displayName, string? sortBy, int pageNumber, int pageSize)
    {
        return await AgentManager.SearchInheritedAgents(groupId, ancestorOrgIds, displayName, sortBy, pageNumber, pageSize).ConfigureAwait(false);
    }

    public async Task<Agent> UpdateAgent(Agent agent)
    {
        return await AgentManager.UpdateAgent(agent).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAgent(Guid id)
    {
        return await AgentManager.DeleteAgent(id).ConfigureAwait(false);
    }

    public async Task<Agent> CloneAgent(Guid agentId, Guid targetOrganizationId, Guid targetGroupId)
    {
        return await AgentManager.CloneAgent(agentId, targetOrganizationId, targetGroupId).ConfigureAwait(false);
    }
}
