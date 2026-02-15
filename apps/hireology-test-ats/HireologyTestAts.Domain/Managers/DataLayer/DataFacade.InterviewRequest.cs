namespace HireologyTestAts.Domain;

internal sealed partial class DataFacade
{
    private InterviewRequestDataManager? _interviewRequestDataManager;
    private InterviewRequestDataManager InterviewRequestDataManager => _interviewRequestDataManager ??= new InterviewRequestDataManager(_dbConnectionString);

    public async Task<InterviewRequest> CreateInterviewRequest(InterviewRequest request)
    {
        return await InterviewRequestDataManager.CreateAsync(request).ConfigureAwait(false);
    }

    public async Task<InterviewRequest?> GetInterviewRequestById(Guid id)
    {
        return await InterviewRequestDataManager.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<InterviewRequest?> GetInterviewRequestByApplicantId(Guid applicantId)
    {
        return await InterviewRequestDataManager.GetByApplicantIdAsync(applicantId).ConfigureAwait(false);
    }

    public async Task<InterviewRequest?> GetInterviewRequestByOrchestratorInterviewId(Guid orchestratorInterviewId)
    {
        return await InterviewRequestDataManager.GetByOrchestratorInterviewIdAsync(orchestratorInterviewId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InterviewRequest>> GetInterviewRequestsByJobId(Guid jobId)
    {
        return await InterviewRequestDataManager.ListByJobIdAsync(jobId).ConfigureAwait(false);
    }

    public async Task<InterviewRequest> UpdateInterviewRequest(InterviewRequest request)
    {
        return await InterviewRequestDataManager.UpdateAsync(request).ConfigureAwait(false);
    }
}
