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

    public Task<PaginatedResult<Agent>> SearchAgents(Guid? organizationId, string? displayName, string? createdBy, string? sortBy, int pageNumber, int pageSize)
    {
        return AgentDataManager.Search(organizationId, displayName, createdBy, sortBy, pageNumber, pageSize);
    }
}
