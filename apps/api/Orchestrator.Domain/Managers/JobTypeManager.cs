namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for JobType entities
/// </summary>
internal sealed class JobTypeManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public JobTypeManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task<JobType> CreateJobType(JobType jobType)
    {
        JobTypeValidator.Validate(jobType);
        return await DataFacade.AddJobType(jobType).ConfigureAwait(false);
    }

    public async Task<JobType?> GetJobTypeById(Guid id)
    {
        return await DataFacade.GetJobTypeById(id).ConfigureAwait(false);
    }

    public async Task<PaginatedResult<JobType>> SearchJobTypes(Guid? organizationId, string? name, bool? isActive, int pageNumber, int pageSize)
    {
        return await DataFacade.SearchJobTypes(organizationId, name, isActive, pageNumber, pageSize).ConfigureAwait(false);
    }

    public async Task<JobType> UpdateJobType(JobType jobType)
    {
        JobTypeValidator.Validate(jobType);
        return await DataFacade.UpdateJobType(jobType).ConfigureAwait(false);
    }

    public async Task<bool> DeleteJobType(Guid id)
    {
        return await DataFacade.DeleteJobType(id).ConfigureAwait(false);
    }

    // Interview Questions management
    public async Task<InterviewQuestion> AddQuestion(InterviewQuestion question)
    {
        JobTypeValidator.ValidateQuestion(question);
        return await DataFacade.AddInterviewQuestion(question).ConfigureAwait(false);
    }

    public async Task<InterviewQuestion?> GetQuestionById(Guid id)
    {
        return await DataFacade.GetInterviewQuestionById(id).ConfigureAwait(false);
    }

    public async Task<IEnumerable<InterviewQuestion>> GetQuestionsByJobTypeId(Guid jobTypeId)
    {
        return await DataFacade.GetInterviewQuestionsByJobTypeId(jobTypeId).ConfigureAwait(false);
    }

    public async Task<InterviewQuestion> UpdateQuestion(InterviewQuestion question)
    {
        JobTypeValidator.ValidateQuestion(question);
        return await DataFacade.UpdateInterviewQuestion(question).ConfigureAwait(false);
    }

    public async Task<bool> DeleteQuestion(Guid id)
    {
        return await DataFacade.DeleteInterviewQuestion(id).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}
