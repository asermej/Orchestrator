namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private InterviewDataManager InterviewDataManager => new(_dbConnectionString);
    private InterviewResponseDataManager InterviewResponseDataManager => new(_dbConnectionString);
    private InterviewResultDataManager InterviewResultDataManager => new(_dbConnectionString);

    // Interview
    public Task<Interview> AddInterview(Interview interview)
    {
        return InterviewDataManager.Add(interview);
    }

    public async Task<Interview?> GetInterviewById(Guid id)
    {
        return await InterviewDataManager.GetById(id);
    }

    public async Task<Interview?> GetInterviewByToken(string token)
    {
        return await InterviewDataManager.GetByToken(token);
    }

    public Task<Interview> UpdateInterview(Interview interview)
    {
        return InterviewDataManager.Update(interview);
    }

    public Task<bool> DeleteInterview(Guid id)
    {
        return InterviewDataManager.Delete(id);
    }

    public Task<PaginatedResult<Interview>> SearchInterviews(Guid? groupId, Guid? jobId, Guid? applicantId, Guid? agentId, string? status, int pageNumber, int pageSize)
    {
        return InterviewDataManager.Search(groupId, jobId, applicantId, agentId, status, pageNumber, pageSize);
    }

    // Interview Responses
    public Task<InterviewResponse> AddInterviewResponse(InterviewResponse response)
    {
        return InterviewResponseDataManager.Add(response);
    }

    public async Task<InterviewResponse?> GetInterviewResponseById(Guid id)
    {
        return await InterviewResponseDataManager.GetById(id);
    }

    public async Task<IEnumerable<InterviewResponse>> GetInterviewResponsesByInterviewId(Guid interviewId)
    {
        return await InterviewResponseDataManager.GetByInterviewId(interviewId);
    }

    public Task<InterviewResponse> UpdateInterviewResponse(InterviewResponse response)
    {
        return InterviewResponseDataManager.Update(response);
    }

    // Interview Results
    public Task<InterviewResult> AddInterviewResult(InterviewResult result)
    {
        return InterviewResultDataManager.Add(result);
    }

    public async Task<InterviewResult?> GetInterviewResultById(Guid id)
    {
        return await InterviewResultDataManager.GetById(id);
    }

    public async Task<InterviewResult?> GetInterviewResultByInterviewId(Guid interviewId)
    {
        return await InterviewResultDataManager.GetByInterviewId(interviewId);
    }

    public Task<InterviewResult> UpdateInterviewResult(InterviewResult result)
    {
        return InterviewResultDataManager.Update(result);
    }
}
