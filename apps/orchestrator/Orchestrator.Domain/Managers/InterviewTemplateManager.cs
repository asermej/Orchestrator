namespace Orchestrator.Domain;

internal sealed class InterviewTemplateManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public InterviewTemplateManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task<InterviewTemplate> CreateTemplate(InterviewTemplate template)
    {
        InterviewTemplateValidator.Validate(template);
        return await DataFacade.AddInterviewTemplate(template).ConfigureAwait(false);
    }

    public async Task<InterviewTemplate?> GetTemplateById(Guid id)
    {
        return await DataFacade.GetInterviewTemplateById(id).ConfigureAwait(false);
    }

    public async Task<PaginatedResult<InterviewTemplate>> SearchTemplates(
        Guid? groupId,
        Guid? agentId,
        string? name,
        bool? isActive,
        string? sortBy,
        int pageNumber,
        int pageSize,
        IReadOnlyList<Guid>? organizationIds = null)
    {
        return await DataFacade.SearchInterviewTemplates(groupId, agentId, name, isActive, sortBy, pageNumber, pageSize, organizationIds).ConfigureAwait(false);
    }

    public async Task<InterviewTemplate> UpdateTemplate(InterviewTemplate template)
    {
        InterviewTemplateValidator.Validate(template);
        return await DataFacade.UpdateInterviewTemplate(template).ConfigureAwait(false);
    }

    public async Task<bool> DeleteTemplate(Guid id, string? deletedBy = null)
    {
        return await DataFacade.DeleteInterviewTemplate(id, deletedBy).ConfigureAwait(false);
    }

    public void Dispose()
    {
    }
}
