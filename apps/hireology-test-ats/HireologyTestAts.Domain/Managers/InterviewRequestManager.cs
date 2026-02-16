namespace HireologyTestAts.Domain;

internal sealed class InterviewRequestManager : IDisposable
{
    private readonly DataFacade _dataFacade;
    private readonly GatewayFacade _gatewayFacade;
    private bool _disposed;

    public InterviewRequestManager(ServiceLocatorBase serviceLocator, GatewayFacade gatewayFacade)
    {
        var configProvider = serviceLocator.CreateConfigurationProvider();
        _dataFacade = new DataFacade(configProvider.GetDbConnectionString());
        _gatewayFacade = gatewayFacade ?? throw new ArgumentNullException(nameof(gatewayFacade));
    }

    /// <summary>
    /// Sends an interview request: syncs applicant, creates interview in Orchestrator, saves local record
    /// </summary>
    public async Task<InterviewRequest> SendInterviewRequest(Guid applicantId, Guid interviewConfigurationId)
    {
        var applicant = await _dataFacade.GetApplicantById(applicantId).ConfigureAwait(false);
        if (applicant == null) throw new ApplicantNotFoundException();

        var job = await _dataFacade.GetJobById(applicant.JobId).ConfigureAwait(false);
        if (job == null) throw new JobNotFoundException();

        var apiKey = await ResolveApiKeyForJob(job).ConfigureAwait(false);

        await _gatewayFacade.SyncApplicant(applicant, job.ExternalJobId, apiKey).ConfigureAwait(false);

        var result = await _gatewayFacade.CreateInterview(
            applicant.Id.ToString(),
            job.ExternalJobId,
            interviewConfigurationId,
            apiKey).ConfigureAwait(false);

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

        var apiKey = await ResolveApiKeyForInterviewRequest(request).ConfigureAwait(false);
        var result = await _gatewayFacade.RefreshInvite(request.OrchestratorInterviewId.Value, apiKey).ConfigureAwait(false);

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

        // Resolve the API key once for the job
        var job = await _dataFacade.GetJobById(jobId).ConfigureAwait(false);
        string? apiKey = null;
        if (job != null)
        {
            apiKey = await ResolveApiKeyForJob(job).ConfigureAwait(false);
        }

        var updated = new List<InterviewRequest>(requests.Count);
        foreach (var request in requests)
        {
            if (request.Status == InterviewRequestStatus.NotStarted && request.OrchestratorInterviewId.HasValue)
            {
                var orchestratorStatus = await _gatewayFacade.GetInterviewStatus(
                    request.OrchestratorInterviewId.Value, apiKey).ConfigureAwait(false);

                if (orchestratorStatus != null)
                {
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
    /// Gets available agents from Orchestrator for a specific group
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorAgent>> GetAgents(string? groupApiKey)
    {
        return await _gatewayFacade.GetAgents(groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets available interview configurations from Orchestrator for a specific group
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorInterviewConfiguration>> GetConfigurations(string? groupApiKey)
    {
        return await _gatewayFacade.GetConfigurations(groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the current webhook status from Orchestrator for a specific group
    /// </summary>
    public async Task<(bool Configured, string? WebhookUrl)> GetWebhookStatus(string? groupApiKey)
    {
        var response = await _gatewayFacade.GetWebhookUrl(groupApiKey).ConfigureAwait(false);
        return (response.Configured, response.WebhookUrl);
    }

    /// <summary>
    /// Configures the webhook URL in Orchestrator for a specific group
    /// </summary>
    public async Task<bool> ConfigureWebhookUrl(string webhookUrl, string? groupApiKey)
    {
        await _gatewayFacade.SetWebhookUrl(webhookUrl, groupApiKey).ConfigureAwait(false);
        return true;
    }

    private async Task<string?> ResolveApiKeyForJob(Job job)
    {
        if (job.OrganizationId.HasValue)
        {
            return await _dataFacade.GetOrchestratorApiKeyForOrganization(job.OrganizationId.Value)
                .ConfigureAwait(false);
        }
        return null;
    }

    private async Task<string?> ResolveApiKeyForInterviewRequest(InterviewRequest request)
    {
        var job = await _dataFacade.GetJobById(request.JobId).ConfigureAwait(false);
        if (job != null)
        {
            return await ResolveApiKeyForJob(job).ConfigureAwait(false);
        }
        return null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
