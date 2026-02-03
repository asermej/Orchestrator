namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private JobTypeDataManager JobTypeDataManager => new(_dbConnectionString);
    private InterviewQuestionDataManager InterviewQuestionDataManager => new(_dbConnectionString);

    public Task<JobType> AddJobType(JobType jobType)
    {
        return JobTypeDataManager.Add(jobType);
    }

    public async Task<JobType?> GetJobTypeById(Guid id)
    {
        return await JobTypeDataManager.GetById(id);
    }

    public Task<JobType> UpdateJobType(JobType jobType)
    {
        return JobTypeDataManager.Update(jobType);
    }

    public Task<bool> DeleteJobType(Guid id)
    {
        return JobTypeDataManager.Delete(id);
    }

    public Task<PaginatedResult<JobType>> SearchJobTypes(Guid? organizationId, string? name, bool? isActive, int pageNumber, int pageSize)
    {
        return JobTypeDataManager.Search(organizationId, name, isActive, pageNumber, pageSize);
    }

    // Interview Questions
    public Task<InterviewQuestion> AddInterviewQuestion(InterviewQuestion question)
    {
        return InterviewQuestionDataManager.Add(question);
    }

    public async Task<InterviewQuestion?> GetInterviewQuestionById(Guid id)
    {
        return await InterviewQuestionDataManager.GetById(id);
    }

    public async Task<IEnumerable<InterviewQuestion>> GetInterviewQuestionsByJobTypeId(Guid jobTypeId)
    {
        return await InterviewQuestionDataManager.GetByJobTypeId(jobTypeId);
    }

    public Task<InterviewQuestion> UpdateInterviewQuestion(InterviewQuestion question)
    {
        return InterviewQuestionDataManager.Update(question);
    }

    public Task<bool> DeleteInterviewQuestion(Guid id)
    {
        return InterviewQuestionDataManager.Delete(id);
    }
}
