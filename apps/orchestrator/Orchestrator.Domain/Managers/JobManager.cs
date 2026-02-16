namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for Job entities
/// </summary>
internal sealed class JobManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public JobManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task<Job> CreateJob(Job job)
    {
        JobValidator.Validate(job);
        return await DataFacade.AddJob(job).ConfigureAwait(false);
    }

    public async Task<Job?> GetJobById(Guid id)
    {
        return await DataFacade.GetJobById(id).ConfigureAwait(false);
    }

    public async Task<Job?> GetJobByExternalId(Guid groupId, string externalJobId)
    {
        return await DataFacade.GetJobByExternalId(groupId, externalJobId).ConfigureAwait(false);
    }

    public async Task<Job> GetOrCreateJob(Guid groupId, string externalJobId, string title, string? description, string? location)
    {
        var existing = await DataFacade.GetJobByExternalId(groupId, externalJobId).ConfigureAwait(false);
        if (existing != null)
        {
            return existing;
        }

        var job = new Job
        {
            GroupId = groupId,
            ExternalJobId = externalJobId,
            Title = title,
            Description = description,
            Location = location
        };

        JobValidator.Validate(job);
        return await DataFacade.AddJob(job).ConfigureAwait(false);
    }

    public async Task<PaginatedResult<Job>> SearchJobs(Guid? groupId, string? title, string? status, int pageNumber, int pageSize, IReadOnlyList<Guid>? organizationIds = null)
    {
        return await DataFacade.SearchJobs(groupId, title, status, pageNumber, pageSize, organizationIds).ConfigureAwait(false);
    }

    public async Task<Job> UpdateJob(Job job)
    {
        JobValidator.Validate(job);
        return await DataFacade.UpdateJob(job).ConfigureAwait(false);
    }

    public async Task<bool> DeleteJob(Guid id)
    {
        return await DataFacade.DeleteJob(id).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}
