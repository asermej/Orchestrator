namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private InterviewTemplateDataManager InterviewTemplateDataManager => new(_dbConnectionString);

    public Task<InterviewTemplate> AddInterviewTemplate(InterviewTemplate template)
    {
        return InterviewTemplateDataManager.Add(template);
    }

    public async Task<InterviewTemplate?> GetInterviewTemplateById(Guid id)
    {
        return await InterviewTemplateDataManager.GetById(id);
    }

    public Task<InterviewTemplate> UpdateInterviewTemplate(InterviewTemplate template)
    {
        return InterviewTemplateDataManager.Update(template);
    }

    public Task<bool> DeleteInterviewTemplate(Guid id, string? deletedBy = null)
    {
        return InterviewTemplateDataManager.Delete(id, deletedBy);
    }

    public Task<PaginatedResult<InterviewTemplate>> SearchInterviewTemplates(
        Guid? groupId,
        Guid? agentId,
        string? name,
        bool? isActive,
        string? sortBy,
        int pageNumber,
        int pageSize,
        IReadOnlyList<Guid>? organizationIds = null)
    {
        return InterviewTemplateDataManager.Search(groupId, agentId, name, isActive, sortBy, pageNumber, pageSize, organizationIds);
    }
}
