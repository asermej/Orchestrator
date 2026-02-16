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
    /// Manually refreshes the status of an interview request by querying Orchestrator.
    /// Use when a webhook may have been missed or delayed.
    /// </summary>
    [HttpPost("interview-requests/{id:guid}/refresh-status")]
    [ProducesResponseType(typeof(InterviewRequestResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewRequestResource>> RefreshStatus(Guid id)
    {
        var request = await _domainFacade.RefreshInterviewRequestStatus(id);
        return Ok(InterviewRequestMapper.ToResource(request));
    }

}
