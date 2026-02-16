namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private JobDataManager JobDataManager => new(_dbConnectionString);

    public Task<Job> AddJob(Job job)
    {
        return JobDataManager.Add(job);
    }

    public async Task<Job?> GetJobById(Guid id)
    {
        return await JobDataManager.GetById(id);
    }

    public async Task<Job?> GetJobByExternalId(Guid groupId, string externalJobId)
    {
        return await JobDataManager.GetByExternalId(groupId, externalJobId);
    }

    public Task<Job> UpdateJob(Job job)
    {
        return JobDataManager.Update(job);
    }

    public Task<bool> DeleteJob(Guid id)
    {
        return JobDataManager.Delete(id);
    }

    public Task<PaginatedResult<Job>> SearchJobs(Guid? groupId, string? title, string? status, int pageNumber, int pageSize, IReadOnlyList<Guid>? organizationIds = null)
    {
        return JobDataManager.Search(groupId, title, status, pageNumber, pageSize, organizationIds);
    }
}
