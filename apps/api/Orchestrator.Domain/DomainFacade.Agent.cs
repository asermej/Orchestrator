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

    public async Task<PaginatedResult<Agent>> SearchAgents(Guid? organizationId, string? displayName, string? createdBy, string? sortBy, int pageNumber, int pageSize)
    {
        return await AgentManager.SearchAgents(organizationId, displayName, createdBy, sortBy, pageNumber, pageSize).ConfigureAwait(false);
    }

    public async Task<Agent> UpdateAgent(Agent agent)
    {
        return await AgentManager.UpdateAgent(agent).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAgent(Guid id)
    {
        return await AgentManager.DeleteAgent(id).ConfigureAwait(false);
    }
}
