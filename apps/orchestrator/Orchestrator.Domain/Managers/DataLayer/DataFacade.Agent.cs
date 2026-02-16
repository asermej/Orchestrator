namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private AgentDataManager AgentDataManager => new(_dbConnectionString);

    public Task<Agent> AddAgent(Agent agent)
    {
        return AgentDataManager.Add(agent);
    }

    public async Task<Agent?> GetAgentById(Guid id)
    {
        return await AgentDataManager.GetById(id);
    }
    
    public Task<Agent> UpdateAgent(Agent agent)
    {
        return AgentDataManager.Update(agent);
    }

    public Task<bool> DeleteAgent(Guid id)
    {
        return AgentDataManager.Delete(id);
    }

    public Task<PaginatedResult<Agent>> SearchAgents(Guid? groupId, string? displayName, string? createdBy, string? sortBy, int pageNumber, int pageSize, IReadOnlyList<Guid>? organizationIds = null)
    {
        return AgentDataManager.Search(groupId, displayName, createdBy, sortBy, pageNumber, pageSize, organizationIds);
    }

    public Task<PaginatedResult<Agent>> SearchLocalAgents(Guid groupId, Guid organizationId, string? displayName, string? sortBy, int pageNumber, int pageSize)
    {
        return AgentDataManager.SearchLocal(groupId, organizationId, displayName, sortBy, pageNumber, pageSize);
    }

    public Task<PaginatedResult<Agent>> SearchInheritedAgents(Guid groupId, IReadOnlyList<Guid> ancestorOrgIds, string? displayName, string? sortBy, int pageNumber, int pageSize)
    {
        return AgentDataManager.SearchInherited(groupId, ancestorOrgIds, displayName, sortBy, pageNumber, pageSize);
    }
}
