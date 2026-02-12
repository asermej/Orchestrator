namespace HireologyTestAts.Domain;

public sealed partial class DomainFacade
{
    public async Task<IReadOnlyList<Job>> GetJobs(int pageNumber, int pageSize, IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        return await JobManager.GetJobs(pageNumber, pageSize, allowedOrganizationIds).ConfigureAwait(false);
    }

    public async Task<int> GetJobCount(IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        return await JobManager.GetJobCount(allowedOrganizationIds).ConfigureAwait(false);
    }

    public async Task<Job> GetJobById(Guid id)
    {
        return await JobManager.GetJobById(id).ConfigureAwait(false);
    }

    public async Task<Job> CreateJob(Job job)
    {
        return await JobManager.CreateJob(job).ConfigureAwait(false);
    }

    public async Task<Job> UpdateJob(Guid id, Job updates)
    {
        return await JobManager.UpdateJob(id, updates).ConfigureAwait(false);
    }

    public async Task<bool> DeleteJob(Guid id)
    {
        return await JobManager.DeleteJob(id).ConfigureAwait(false);
    }
}
