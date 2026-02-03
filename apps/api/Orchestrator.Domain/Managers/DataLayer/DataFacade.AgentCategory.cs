using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private AgentCategoryDataManager AgentCategoryDataManager => new(_dbConnectionString);

    public Task<AgentCategory> AddAgentCategory(AgentCategory agentCategory)
    {
        return AgentCategoryDataManager.Add(agentCategory);
    }

    public async Task<AgentCategory?> GetAgentCategoryById(Guid id)
    {
        return await AgentCategoryDataManager.GetById(id);
    }
    
    public Task<bool> DeleteAgentCategory(Guid id)
    {
        return AgentCategoryDataManager.Delete(id);
    }

    public Task<bool> DeleteAgentCategoryByAgentAndCategory(Guid agentId, Guid categoryId)
    {
        return AgentCategoryDataManager.DeleteByAgentAndCategory(agentId, categoryId);
    }

    public async Task<IEnumerable<AgentCategory>> GetAgentCategoriesByAgentId(Guid agentId)
    {
        return await AgentCategoryDataManager.GetByAgentId(agentId);
    }

    public async Task<IEnumerable<AgentCategory>> GetAgentCategoriesByCategoryId(Guid categoryId)
    {
        return await AgentCategoryDataManager.GetByCategoryId(categoryId);
    }

    public async Task<AgentCategory?> GetAgentCategoryByAgentAndCategory(Guid agentId, Guid categoryId)
    {
        return await AgentCategoryDataManager.GetByAgentAndCategory(agentId, categoryId);
    }

    public async Task<IEnumerable<Category>> GetCategoriesByAgentId(Guid agentId)
    {
        return await AgentCategoryDataManager.GetCategoriesByAgentId(agentId);
    }
}

