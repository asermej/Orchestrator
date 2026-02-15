namespace HireologyTestAts.Domain;

internal sealed class InterviewRequestManager : IDisposable
{
    private readonly DataFacade _dataFacade;
    private readonly OrchestratorGateway _orchestratorGateway;
    private bool _disposed;

    public InterviewRequestManager(ServiceLocatorBase serviceLocator)
    {
        var configProvider = serviceLocator.CreateConfigurationProvider();
        _dataFacade = new DataFacade(configProvider.GetDbConnectionString());
        _orchestratorGateway = new OrchestratorGateway(
            configProvider.GetOrchestratorBaseUrl(),
            configProvider.GetOrchestratorApiKey());
    }

    /// <summary>
    /// Sends an interview request: syncs applicant, creates interview in Orchestrator, saves local record
    /// </summary>
    public async Task<InterviewRequest> SendInterviewRequest(Guid applicantId, Guid interviewConfigurationId)
    {
        // Get applicant and job
        var applicant = await _dataFacade.GetApplicantById(applicantId).ConfigureAwait(false);
        if (applicant == null) throw new ApplicantNotFoundException();

        var job = await _dataFacade.GetJobById(applicant.JobId).ConfigureAwait(false);
        if (job == null) throw new JobNotFoundException();

        // Sync applicant to Orchestrator
        await _orchestratorGateway.SyncApplicantAsync(
            applicant, job.ExternalJobId).ConfigureAwait(false);

        // Create interview in Orchestrator with the selected configuration
        var result = await _orchestratorGateway.CreateInterviewAsync(
            applicant.Id.ToString(),
            job.ExternalJobId,
            interviewConfigurationId).ConfigureAwait(false);

        // Save local record
        var request = new InterviewRequest
        {
            ApplicantId = applicantId,
            JobId = applicant.JobId,
            OrchestratorInterviewId = result.InterviewId,
            InviteUrl = result.InviteUrl,
            ShortCode = result.ShortCode,
            Status = InterviewRequestStatus.NotStarted
        };

        return await _dataFacade.CreateInterviewRequest(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Refreshes the invite for an existing interview request (when the link is expired or used up)
    /// </summary>
    public async Task<InterviewRequest> RefreshInvite(Guid interviewRequestId)
    {
        var request = await _dataFacade.GetInterviewRequestById(interviewRequestId).ConfigureAwait(false);
        if (request == null) throw new InterviewRequestNotFoundException();
        if (request.OrchestratorInterviewId == null)
            throw new InterviewRequestValidationException("No Orchestrator interview linked to this request");

        var result = await _orchestratorGateway.RefreshInviteAsync(request.OrchestratorInterviewId.Value).ConfigureAwait(false);

        request.InviteUrl = result.InviteUrl;
        request.ShortCode = result.ShortCode;
        request.Status = InterviewRequestStatus.NotStarted;

        return await _dataFacade.UpdateInterviewRequest(request).ConfigureAwait(false);
    }

    public async Task<InterviewRequest?> GetByApplicantId(Guid applicantId)
    {
        return await _dataFacade.GetInterviewRequestByApplicantId(applicantId).ConfigureAwait(false);
    }

    public async Task<InterviewRequest?> GetById(Guid id)
    {
        return await _dataFacade.GetInterviewRequestById(id).ConfigureAwait(false);
    }

    public async Task<InterviewRequest?> GetByOrchestratorInterviewId(Guid orchestratorInterviewId)
    {
        return await _dataFacade.GetInterviewRequestByOrchestratorInterviewId(orchestratorInterviewId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InterviewRequest>> GetByJobId(Guid jobId)
    {
        var requests = await _dataFacade.GetInterviewRequestsByJobId(jobId).ConfigureAwait(false);

        // For any not_started requests, check the Orchestrator to see if the invite is still valid
        var updated = new List<InterviewRequest>(requests.Count);
        foreach (var request in requests)
        {
            if (request.Status == InterviewRequestStatus.NotStarted && request.OrchestratorInterviewId.HasValue)
            {
                var orchestratorStatus = await _orchestratorGateway.GetInterviewStatusAsync(
                    request.OrchestratorInterviewId.Value).ConfigureAwait(false);

                if (orchestratorStatus != null)
                {
                    // Update local status based on Orchestrator invite status
                    if (orchestratorStatus.InviteStatus is "max_uses_reached" or "expired" or "revoked")
                    {
                        request.Status = InterviewRequestStatus.LinkExpired;
                        await _dataFacade.UpdateInterviewRequest(request).ConfigureAwait(false);
                    }
                    else if (orchestratorStatus.InterviewStatus == "in_progress")
                    {
                        request.Status = InterviewRequestStatus.InProgress;
                        await _dataFacade.UpdateInterviewRequest(request).ConfigureAwait(false);
                    }
                }
            }
            updated.Add(request);
        }

        return updated;
    }

    /// <summary>
    /// Updates status and result from webhook payload
    /// </summary>
    public async Task<InterviewRequest> UpdateFromWebhook(
        Guid orchestratorInterviewId,
        string status,
        int? score,
        string? summary,
        string? recommendation,
        string? strengths,
        string? areasForImprovement)
    {
        var request = await _dataFacade.GetInterviewRequestByOrchestratorInterviewId(orchestratorInterviewId)
            .ConfigureAwait(false);

        if (request == null)
            throw new InterviewRequestNotFoundException();

        request.Status = status;
        request.Score = score;
        request.ResultSummary = summary;
        request.ResultRecommendation = recommendation;
        request.ResultStrengths = strengths;
        request.ResultAreasForImprovement = areasForImprovement;
        request.WebhookReceivedAt = DateTime.UtcNow;

        return await _dataFacade.UpdateInterviewRequest(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets available agents from Orchestrator
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorAgent>> GetAgents()
    {
        return await _orchestratorGateway.GetAgentsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets available interview configurations from Orchestrator
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorInterviewConfiguration>> GetConfigurations()
    {
        return await _orchestratorGateway.GetConfigurationsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Configures the webhook URL in Orchestrator for this organization
    /// </summary>
    public async Task<bool> ConfigureWebhookUrl(string webhookUrl)
    {
        return await _orchestratorGateway.SetWebhookUrlAsync(webhookUrl).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _orchestratorGateway.Dispose();
            _disposed = true;
        }
    }
}
