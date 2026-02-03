namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Creates a new JobType
    /// </summary>
    public async Task<JobType> CreateJobType(JobType jobType)
    {
        return await JobTypeManager.CreateJobType(jobType).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a JobType by ID
    /// </summary>
    public async Task<JobType?> GetJobTypeById(Guid id)
    {
        return await JobTypeManager.GetJobTypeById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for JobTypes
    /// </summary>
    public async Task<PaginatedResult<JobType>> SearchJobTypes(Guid? organizationId, string? name, bool? isActive, int pageNumber, int pageSize)
    {
        return await JobTypeManager.SearchJobTypes(organizationId, name, isActive, pageNumber, pageSize).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a JobType
    /// </summary>
    public async Task<JobType> UpdateJobType(JobType jobType)
    {
        return await JobTypeManager.UpdateJobType(jobType).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a JobType
    /// </summary>
    public async Task<bool> DeleteJobType(Guid id)
    {
        return await JobTypeManager.DeleteJobType(id).ConfigureAwait(false);
    }

    // Interview Questions

    /// <summary>
    /// Adds a question to a JobType
    /// </summary>
    public async Task<InterviewQuestion> AddInterviewQuestion(InterviewQuestion question)
    {
        return await JobTypeManager.AddQuestion(question).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an InterviewQuestion by ID
    /// </summary>
    public async Task<InterviewQuestion?> GetInterviewQuestionById(Guid id)
    {
        return await JobTypeManager.GetQuestionById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all questions for a JobType
    /// </summary>
    public async Task<IEnumerable<InterviewQuestion>> GetInterviewQuestionsByJobTypeId(Guid jobTypeId)
    {
        return await JobTypeManager.GetQuestionsByJobTypeId(jobTypeId).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an InterviewQuestion
    /// </summary>
    public async Task<InterviewQuestion> UpdateInterviewQuestion(InterviewQuestion question)
    {
        return await JobTypeManager.UpdateQuestion(question).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an InterviewQuestion
    /// </summary>
    public async Task<bool> DeleteInterviewQuestion(Guid id)
    {
        return await JobTypeManager.DeleteQuestion(id).ConfigureAwait(false);
    }
}
