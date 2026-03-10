namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    public async Task<InterviewTemplate> CreateInterviewTemplate(InterviewTemplate template)
    {
        return await InterviewTemplateManager.CreateTemplate(template).ConfigureAwait(false);
    }

    public async Task<InterviewTemplate?> GetInterviewTemplateById(Guid id)
    {
        return await InterviewTemplateManager.GetTemplateById(id).ConfigureAwait(false);
    }

    public async Task<PaginatedResult<InterviewTemplate>> SearchInterviewTemplates(
        Guid? groupId,
        Guid? agentId,
        string? name,
        bool? isActive,
        string? sortBy,
        int pageNumber,
        int pageSize,
        IReadOnlyList<Guid>? organizationIds = null)
    {
        return await InterviewTemplateManager.SearchTemplates(groupId, agentId, name, isActive, sortBy, pageNumber, pageSize, organizationIds).ConfigureAwait(false);
    }

    public async Task<InterviewTemplate> UpdateInterviewTemplate(InterviewTemplate template)
    {
        return await InterviewTemplateManager.UpdateTemplate(template).ConfigureAwait(false);
    }

    public async Task<bool> DeleteInterviewTemplate(Guid id, string? deletedBy = null)
    {
        return await InterviewTemplateManager.DeleteTemplate(id, deletedBy).ConfigureAwait(false);
    }
}
