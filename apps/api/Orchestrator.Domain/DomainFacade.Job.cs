namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Creates a new Job
    /// </summary>
    public async Task<Job> CreateJob(Job job)
    {
        return await JobManager.CreateJob(job).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a Job by ID
    /// </summary>
    public async Task<Job?> GetJobById(Guid id)
    {
        return await JobManager.GetJobById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a Job by external ID
    /// </summary>
    public async Task<Job?> GetJobByExternalId(Guid organizationId, string externalJobId)
    {
        return await JobManager.GetJobByExternalId(organizationId, externalJobId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets or creates a Job by external ID
    /// </summary>
    public async Task<Job> GetOrCreateJob(Guid organizationId, string externalJobId, string title, string? description, string? location)
    {
        return await JobManager.GetOrCreateJob(organizationId, externalJobId, title, description, location).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for Jobs
    /// </summary>
    public async Task<PaginatedResult<Job>> SearchJobs(Guid? organizationId, string? title, string? status, int pageNumber, int pageSize)
    {
        return await JobManager.SearchJobs(organizationId, title, status, pageNumber, pageSize).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a Job
    /// </summary>
    public async Task<Job> UpdateJob(Job job)
    {
        return await JobManager.UpdateJob(job).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a Job
    /// </summary>
    public async Task<bool> DeleteJob(Guid id)
    {
        return await JobManager.DeleteJob(id).ConfigureAwait(false);
    }
}
