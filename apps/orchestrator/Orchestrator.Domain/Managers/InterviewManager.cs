namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for Interview entities
/// </summary>
internal sealed class InterviewManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public InterviewManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task<Interview> CreateInterview(Interview interview)
    {
        // Generate token if not provided
        if (string.IsNullOrEmpty(interview.Token))
        {
            interview.Token = GenerateInterviewToken();
        }

        InterviewValidator.Validate(interview);
        return await DataFacade.AddInterview(interview).ConfigureAwait(false);
    }

    public async Task<Interview?> GetInterviewById(Guid id)
    {
        return await DataFacade.GetInterviewById(id).ConfigureAwait(false);
    }

    public async Task<Interview?> GetInterviewByToken(string token)
    {
        return await DataFacade.GetInterviewByToken(token).ConfigureAwait(false);
    }

    public async Task<PaginatedResult<Interview>> SearchInterviews(Guid? jobId, Guid? applicantId, Guid? agentId, string? status, int pageNumber, int pageSize)
    {
        return await DataFacade.SearchInterviews(jobId, applicantId, agentId, status, pageNumber, pageSize).ConfigureAwait(false);
    }

    public async Task<Interview> UpdateInterview(Interview interview)
    {
        return await DataFacade.UpdateInterview(interview).ConfigureAwait(false);
    }

    public async Task<Interview> StartInterview(Guid interviewId)
    {
        var interview = await DataFacade.GetInterviewById(interviewId).ConfigureAwait(false);
        if (interview == null)
        {
            throw new InterviewNotFoundException($"Interview with ID {interviewId} not found.");
        }

        interview.Status = InterviewStatus.InProgress;
        interview.StartedAt = DateTime.UtcNow;
        return await DataFacade.UpdateInterview(interview).ConfigureAwait(false);
    }

    public async Task<Interview> CompleteInterview(Guid interviewId)
    {
        var interview = await DataFacade.GetInterviewById(interviewId).ConfigureAwait(false);
        if (interview == null)
        {
            throw new InterviewNotFoundException($"Interview with ID {interviewId} not found.");
        }

        interview.Status = InterviewStatus.Completed;
        interview.CompletedAt = DateTime.UtcNow;
        return await DataFacade.UpdateInterview(interview).ConfigureAwait(false);
    }

    public async Task<bool> DeleteInterview(Guid id)
    {
        return await DataFacade.DeleteInterview(id).ConfigureAwait(false);
    }

    // Interview Responses
    public async Task<InterviewResponse> AddResponse(InterviewResponse response)
    {
        InterviewValidator.ValidateResponse(response);
        return await DataFacade.AddInterviewResponse(response).ConfigureAwait(false);
    }

    public async Task<InterviewResponse?> GetResponseById(Guid id)
    {
        return await DataFacade.GetInterviewResponseById(id).ConfigureAwait(false);
    }

    public async Task<IEnumerable<InterviewResponse>> GetResponsesByInterviewId(Guid interviewId)
    {
        return await DataFacade.GetInterviewResponsesByInterviewId(interviewId).ConfigureAwait(false);
    }

    public async Task<InterviewResponse> UpdateResponse(InterviewResponse response)
    {
        return await DataFacade.UpdateInterviewResponse(response).ConfigureAwait(false);
    }

    // Interview Results
    public async Task<InterviewResult> CreateResult(InterviewResult result)
    {
        InterviewValidator.ValidateResult(result);
        return await DataFacade.AddInterviewResult(result).ConfigureAwait(false);
    }

    public async Task<InterviewResult?> GetResultById(Guid id)
    {
        return await DataFacade.GetInterviewResultById(id).ConfigureAwait(false);
    }

    public async Task<InterviewResult?> GetResultByInterviewId(Guid interviewId)
    {
        return await DataFacade.GetInterviewResultByInterviewId(interviewId).ConfigureAwait(false);
    }

    public async Task<InterviewResult> UpdateResult(InterviewResult result)
    {
        return await DataFacade.UpdateInterviewResult(result).ConfigureAwait(false);
    }

    private static string GenerateInterviewToken()
    {
        var bytes = new byte[24];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "");
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}
