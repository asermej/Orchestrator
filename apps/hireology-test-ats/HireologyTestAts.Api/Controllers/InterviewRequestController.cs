using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.Mappers;
using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
[Authorize]
public class InterviewRequestController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public InterviewRequestController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Lists available interview agents from Orchestrator
    /// </summary>
    [HttpGet("agents")]
    [ProducesResponseType(typeof(IReadOnlyList<AgentResource>), 200)]
    public async Task<ActionResult<IReadOnlyList<AgentResource>>> ListAgents()
    {
        var agents = await _domainFacade.GetAgents();
        return Ok(InterviewRequestMapper.ToAgentResource(agents));
    }

    /// <summary>
    /// Lists available interview configurations from Orchestrator.
    /// Each configuration defines the agent, questions, and scoring rubric.
    /// </summary>
    [HttpGet("configurations")]
    [ProducesResponseType(typeof(IReadOnlyList<InterviewConfigurationResource>), 200)]
    public async Task<ActionResult<IReadOnlyList<InterviewConfigurationResource>>> ListConfigurations()
    {
        var configs = await _domainFacade.GetInterviewConfigurations();
        return Ok(InterviewRequestMapper.ToConfigurationResource(configs));
    }

    /// <summary>
    /// Sends an interview request for an applicant. Creates the interview in Orchestrator and returns the invite URL.
    /// </summary>
    [HttpPost("applicants/{applicantId:guid}/interview")]
    [ProducesResponseType(typeof(InterviewRequestResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewRequestResource>> SendInterview(Guid applicantId, [FromBody] SendInterviewRequestResource resource)
    {
        if (resource.InterviewConfigurationId == Guid.Empty)
        {
            return BadRequest("InterviewConfigurationId is required");
        }

        var request = await _domainFacade.SendInterviewRequest(applicantId, resource.InterviewConfigurationId);
        return Created($"/api/v1/interview-requests/{request.Id}", InterviewRequestMapper.ToResource(request));
    }

    /// <summary>
    /// Gets the most recent interview request for an applicant
    /// </summary>
    [HttpGet("applicants/{applicantId:guid}/interview")]
    [ProducesResponseType(typeof(InterviewRequestResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewRequestResource>> GetInterviewByApplicant(Guid applicantId)
    {
        var request = await _domainFacade.GetInterviewRequestByApplicantId(applicantId);
        if (request == null)
        {
            return NotFound("No interview request found for this applicant");
        }
        return Ok(InterviewRequestMapper.ToResource(request));
    }

    /// <summary>
    /// Gets all interview requests for a job
    /// </summary>
    [HttpGet("jobs/{jobId:guid}/interviews")]
    [ProducesResponseType(typeof(IReadOnlyList<InterviewRequestResource>), 200)]
    public async Task<ActionResult<IReadOnlyList<InterviewRequestResource>>> GetInterviewsByJob(Guid jobId)
    {
        var requests = await _domainFacade.GetInterviewRequestsByJobId(jobId);
        return Ok(InterviewRequestMapper.ToResource(requests));
    }

    /// <summary>
    /// Refreshes the invite link for an existing interview request (when the original link expired or was used up)
    /// </summary>
    [HttpPost("interview-requests/{id:guid}/refresh-invite")]
    [ProducesResponseType(typeof(InterviewRequestResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewRequestResource>> RefreshInvite(Guid id)
    {
        var request = await _domainFacade.RefreshInterviewInvite(id);
        return Ok(InterviewRequestMapper.ToResource(request));
    }

    /// <summary>
    /// Gets an interview request by ID
    /// </summary>
    [HttpGet("interview-requests/{id:guid}")]
    [ProducesResponseType(typeof(InterviewRequestResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewRequestResource>> GetInterviewRequest(Guid id)
    {
        var request = await _domainFacade.GetInterviewRequestById(id);
        if (request == null)
        {
            return NotFound("Interview request not found");
        }
        return Ok(InterviewRequestMapper.ToResource(request));
    }

    /// <summary>
    /// Gets the current webhook configuration status from Orchestrator
    /// </summary>
    [HttpGet("settings/webhook-status")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetWebhookStatus()
    {
        var (configured, webhookUrl) = await _domainFacade.GetWebhookStatus();
        return Ok(new { configured, webhookUrl });
    }

    /// <summary>
    /// Configures the webhook URL in Orchestrator so interview results are sent back to this ATS
    /// </summary>
    [HttpPost("settings/configure-webhook")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> ConfigureWebhook()
    {
        // Determine our own webhook URL based on the request
        var scheme = Request.Scheme;
        var host = Request.Host;
        var webhookUrl = $"{scheme}://{host}/api/v1/webhooks/orchestrator";

        var success = await _domainFacade.ConfigureWebhookUrl(webhookUrl);
        if (!success)
        {
            return BadRequest("Failed to configure webhook URL in Orchestrator");
        }

        return Ok(new { webhookUrl, configured = true });
    }
}
