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

    public async Task<Job?> GetJobByExternalId(Guid organizationId, string externalJobId)
    {
        return await JobDataManager.GetByExternalId(organizationId, externalJobId);
    }

    public Task<Job> UpdateJob(Job job)
    {
        return JobDataManager.Update(job);
    }

    public Task<bool> DeleteJob(Guid id)
    {
        return JobDataManager.Delete(id);
    }

    public Task<PaginatedResult<Job>> SearchJobs(Guid? organizationId, Guid? jobTypeId, string? title, string? status, int pageNumber, int pageSize)
    {
        return JobDataManager.Search(organizationId, jobTypeId, title, status, pageNumber, pageSize);
    }
}
