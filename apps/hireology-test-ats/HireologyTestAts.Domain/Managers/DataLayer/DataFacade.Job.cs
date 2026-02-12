namespace HireologyTestAts.Domain;

internal sealed partial class DataFacade
{
    private JobDataManager? _jobDataManager;
    private JobDataManager JobDataManager => _jobDataManager ??= new JobDataManager(_dbConnectionString);

    public async Task<IReadOnlyList<Job>> GetJobs(int pageNumber, int pageSize, IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        return await JobDataManager.ListAsync(pageNumber, pageSize, allowedOrganizationIds).ConfigureAwait(false);
    }

    public async Task<int> GetJobCount(IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        return await JobDataManager.CountAsync(allowedOrganizationIds).ConfigureAwait(false);
    }

    public async Task<Job?> GetJobById(Guid id)
    {
        return await JobDataManager.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<Job?> GetJobByExternalId(string externalJobId)
    {
        return await JobDataManager.GetByExternalIdAsync(externalJobId).ConfigureAwait(false);
    }

    public async Task<Job> CreateJob(Job job)
    {
        return await JobDataManager.CreateAsync(job).ConfigureAwait(false);
    }

    public async Task<Job?> UpdateJob(Job job)
    {
        return await JobDataManager.UpdateAsync(job).ConfigureAwait(false);
    }

    public async Task<bool> DeleteJob(Guid id)
    {
        return await JobDataManager.DeleteAsync(id).ConfigureAwait(false);
    }
}
