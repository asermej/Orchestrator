namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private InterviewConfigurationDataManager InterviewConfigurationDataManager => new(_dbConnectionString);

    public Task<InterviewConfiguration> AddInterviewConfiguration(InterviewConfiguration config)
    {
        return InterviewConfigurationDataManager.Add(config);
    }

    public async Task<InterviewConfiguration?> GetInterviewConfigurationById(Guid id)
    {
        return await InterviewConfigurationDataManager.GetById(id);
    }

    public Task<InterviewConfiguration> UpdateInterviewConfiguration(InterviewConfiguration config)
    {
        return InterviewConfigurationDataManager.Update(config);
    }

    public Task<bool> DeleteInterviewConfiguration(Guid id, string? deletedBy = null)
    {
        return InterviewConfigurationDataManager.Delete(id, deletedBy);
    }

    public Task<PaginatedResult<InterviewConfiguration>> SearchInterviewConfigurations(
        Guid? groupId, 
        Guid? agentId, 
        string? name, 
        bool? isActive,
        string? sortBy, 
        int pageNumber, 
        int pageSize,
        IReadOnlyList<Guid>? organizationIds = null)
    {
        return InterviewConfigurationDataManager.Search(groupId, agentId, name, isActive, sortBy, pageNumber, pageSize, organizationIds);
    }
}
