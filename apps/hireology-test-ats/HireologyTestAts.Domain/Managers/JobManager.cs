namespace HireologyTestAts.Domain;

internal sealed class JobManager : IDisposable
{
    private readonly DataFacade _dataFacade;
    private readonly OrchestratorGateway _orchestratorGateway;
    private bool _disposed;

    public JobManager(ServiceLocatorBase serviceLocator)
    {
        var configProvider = serviceLocator.CreateConfigurationProvider();
        _dataFacade = new DataFacade(configProvider.GetDbConnectionString());
        _orchestratorGateway = new OrchestratorGateway(
            configProvider.GetOrchestratorBaseUrl(),
            configProvider.GetOrchestratorApiKey());
    }

    public async Task<IReadOnlyList<Job>> GetJobs(int pageNumber, int pageSize, IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        return await _dataFacade.GetJobs(pageNumber, pageSize, allowedOrganizationIds).ConfigureAwait(false);
    }

    public async Task<int> GetJobCount(IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        return await _dataFacade.GetJobCount(allowedOrganizationIds).ConfigureAwait(false);
    }

    public async Task<Job> GetJobById(Guid id)
    {
        var job = await _dataFacade.GetJobById(id).ConfigureAwait(false);
        if (job == null) throw new JobNotFoundException();
        return job;
    }

    public async Task<Job> CreateJob(Job job)
    {
        if (string.IsNullOrWhiteSpace(job.ExternalJobId))
            throw new JobValidationException("ExternalJobId is required");
        if (string.IsNullOrWhiteSpace(job.Title))
            throw new JobValidationException("Title is required");

        job.ExternalJobId = job.ExternalJobId.Trim();
        job.Title = job.Title.Trim();
        job.Description = job.Description?.Trim();
        job.Location = job.Location?.Trim();
        job.Status = job.Status ?? "active";

        var existing = await _dataFacade.GetJobByExternalId(job.ExternalJobId).ConfigureAwait(false);
        if (existing != null)
            throw new JobValidationException($"Job with ExternalJobId '{job.ExternalJobId}' already exists");

        var created = await _dataFacade.CreateJob(job).ConfigureAwait(false);

        await _orchestratorGateway.SyncJobAsync(created).ConfigureAwait(false);

        return created;
    }

    public async Task<Job> UpdateJob(Guid id, Job updates)
    {
        var existing = await _dataFacade.GetJobById(id).ConfigureAwait(false);
        if (existing == null) throw new JobNotFoundException();

        existing.Title = updates.Title ?? existing.Title;
        existing.Description = updates.Description ?? existing.Description;
        existing.Location = updates.Location ?? existing.Location;
        existing.Status = updates.Status ?? existing.Status;
        if (updates.OrganizationId.HasValue)
            existing.OrganizationId = updates.OrganizationId;

        var updated = await _dataFacade.UpdateJob(existing).ConfigureAwait(false);
        if (updated == null) throw new JobNotFoundException();

        await _orchestratorGateway.SyncJobAsync(updated).ConfigureAwait(false);

        return updated;
    }

    public async Task<bool> DeleteJob(Guid id)
    {
        var existing = await _dataFacade.GetJobById(id).ConfigureAwait(false);
        if (existing == null) throw new JobNotFoundException();

        var externalId = existing.ExternalJobId;
        var deleted = await _dataFacade.DeleteJob(id).ConfigureAwait(false);
        if (!deleted) throw new JobNotFoundException();

        await _orchestratorGateway.DeleteJobAsync(externalId).ConfigureAwait(false);

        return true;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _orchestratorGateway.Dispose();
            _disposed = true;
        }
    }
}
