using Microsoft.AspNetCore.Mvc;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for ATS integration endpoints (uses API key authentication)
/// </summary>
[ApiController]
[Route("api/v1/ats")]
[Produces("application/json")]
public class AtsController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public AtsController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    private Guid GetOrganizationId()
    {
        if (HttpContext.Items.TryGetValue("OrganizationId", out var orgId) && orgId is Guid id)
        {
            return id;
        }
        throw new UnauthorizedAccessException("Organization not found in context");
    }

    // Settings Endpoints

    /// <summary>
    /// Updates the webhook URL for the authenticated organization
    /// </summary>
    [HttpPut("settings/webhook")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> UpdateWebhookUrl([FromBody] UpdateWebhookResource resource)
    {
        var organizationId = GetOrganizationId();
        var organization = await _domainFacade.GetOrganizationById(organizationId);
        if (organization == null)
        {
            return NotFound("Organization not found");
        }

        organization.WebhookUrl = resource.WebhookUrl;
        await _domainFacade.UpdateOrganization(organization);

        return Ok(new { webhookUrl = organization.WebhookUrl });
    }

    // Job Endpoints

    /// <summary>
    /// Creates or updates a job from ATS
    /// </summary>
    [HttpPost("jobs")]
    [ProducesResponseType(typeof(JobResource), 201)]
    [ProducesResponseType(typeof(JobResource), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<JobResource>> CreateOrUpdateJob([FromBody] CreateJobResource resource)
    {
        var organizationId = GetOrganizationId();

        // Check if job already exists
        var existing = await _domainFacade.GetJobByExternalId(organizationId, resource.ExternalJobId);
        if (existing != null)
        {
            // Update existing job
            var update = new UpdateJobResource
            {
                Title = resource.Title,
                Description = resource.Description,
                Location = resource.Location
            };
            var updatedJob = JobMapper.ToDomain(update, existing);
            var updated = await _domainFacade.UpdateJob(updatedJob);
            return Ok(JobMapper.ToResource(updated));
        }

        // Create new job
        var job = JobMapper.ToDomain(resource, organizationId);
        var created = await _domainFacade.CreateJob(job);
        return Created($"/api/v1/ats/jobs/{created.ExternalJobId}", JobMapper.ToResource(created));
    }

    /// <summary>
    /// Gets a job by external ID
    /// </summary>
    [HttpGet("jobs/{externalJobId}")]
    [ProducesResponseType(typeof(JobResource), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<JobResource>> GetJob(string externalJobId)
    {
        var organizationId = GetOrganizationId();
        var job = await _domainFacade.GetJobByExternalId(organizationId, externalJobId);
        if (job == null)
        {
            return NotFound($"Job with external ID {externalJobId} not found");
        }
        return Ok(JobMapper.ToResource(job));
    }

    /// <summary>
    /// Searches for jobs
    /// </summary>
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(PaginatedResponse<JobResource>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<PaginatedResponse<JobResource>>> SearchJobs([FromQuery] SearchJobRequest request)
    {
        var organizationId = GetOrganizationId();
        var result = await _domainFacade.SearchJobs(
            organizationId,
            request.Title,
            request.Status,
            request.PageNumber,
            request.PageSize);

        return Ok(new PaginatedResponse<JobResource>
        {
            Items = JobMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        });
    }

    /// <summary>
    /// Deletes a job by external ID (soft delete in Orchestrator)
    /// </summary>
    [HttpDelete("jobs/{externalJobId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> DeleteJob(string externalJobId)
    {
        var organizationId = GetOrganizationId();
        var job = await _domainFacade.GetJobByExternalId(organizationId, externalJobId);
        if (job == null)
        {
            return NotFound($"Job with external ID {externalJobId} not found");
        }
        await _domainFacade.DeleteJob(job.Id);
        return NoContent();
    }

    // Applicant Endpoints

    /// <summary>
    /// Creates or updates an applicant from ATS
    /// </summary>
    [HttpPost("applicants")]
    [ProducesResponseType(typeof(ApplicantResource), 201)]
    [ProducesResponseType(typeof(ApplicantResource), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<ApplicantResource>> CreateOrUpdateApplicant([FromBody] CreateApplicantResource resource)
    {
        var organizationId = GetOrganizationId();

        // Check if applicant already exists
        var existing = await _domainFacade.GetApplicantByExternalId(organizationId, resource.ExternalApplicantId);
        if (existing != null)
        {
            // Update existing applicant
            var update = new UpdateApplicantResource
            {
                FirstName = resource.FirstName,
                LastName = resource.LastName,
                Email = resource.Email,
                Phone = resource.Phone
            };
            var updatedApplicant = ApplicantMapper.ToDomain(update, existing);
            var updated = await _domainFacade.UpdateApplicant(updatedApplicant);
            return Ok(ApplicantMapper.ToResource(updated));
        }

        // Create new applicant
        var applicant = ApplicantMapper.ToDomain(resource, organizationId);
        var created = await _domainFacade.CreateApplicant(applicant);
        return Created($"/api/v1/ats/applicants/{created.ExternalApplicantId}", ApplicantMapper.ToResource(created));
    }

    /// <summary>
    /// Gets an applicant by external ID
    /// </summary>
    [HttpGet("applicants/{externalApplicantId}")]
    [ProducesResponseType(typeof(ApplicantResource), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<ApplicantResource>> GetApplicant(string externalApplicantId)
    {
        var organizationId = GetOrganizationId();
        var applicant = await _domainFacade.GetApplicantByExternalId(organizationId, externalApplicantId);
        if (applicant == null)
        {
            return NotFound($"Applicant with external ID {externalApplicantId} not found");
        }
        return Ok(ApplicantMapper.ToResource(applicant));
    }

    /// <summary>
    /// Searches for applicants
    /// </summary>
    [HttpGet("applicants")]
    [ProducesResponseType(typeof(PaginatedResponse<ApplicantResource>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<PaginatedResponse<ApplicantResource>>> SearchApplicants([FromQuery] SearchApplicantRequest request)
    {
        var organizationId = GetOrganizationId();
        var result = await _domainFacade.SearchApplicants(
            organizationId,
            request.Email,
            request.Name,
            request.PageNumber,
            request.PageSize);

        return Ok(new PaginatedResponse<ApplicantResource>
        {
            Items = ApplicantMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        });
    }

    // Agent Endpoints

    /// <summary>
    /// Lists agents available for the authenticated organization
    /// </summary>
    [HttpGet("agents")]
    [ProducesResponseType(typeof(IEnumerable<AtsAgentResource>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<IEnumerable<AtsAgentResource>>> ListAgents()
    {
        var organizationId = GetOrganizationId();
        var result = await _domainFacade.SearchAgents(
            organizationId, null, null, null, 1, 100);

        var resources = result.Items.Select(a => new AtsAgentResource
        {
            Id = a.Id,
            DisplayName = a.DisplayName,
            ProfileImageUrl = a.ProfileImageUrl
        });

        return Ok(resources);
    }

    // Interview Configuration Endpoints

    /// <summary>
    /// Lists interview configurations available for the authenticated organization.
    /// Each configuration defines the agent, questions, and scoring rubric for an interview.
    /// </summary>
    [HttpGet("configurations")]
    [ProducesResponseType(typeof(IEnumerable<AtsInterviewConfigurationResource>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<IEnumerable<AtsInterviewConfigurationResource>>> ListConfigurations()
    {
        var organizationId = GetOrganizationId();
        var result = await _domainFacade.SearchInterviewConfigurations(
            organizationId, null, null, true, null, 1, 100);

        // Fetch agent names for display
        var agentIds = result.Items.Select(c => c.AgentId).Distinct().ToList();
        var agentNames = new Dictionary<Guid, string>();
        foreach (var agentId in agentIds)
        {
            var agent = await _domainFacade.GetAgentById(agentId);
            if (agent != null)
            {
                agentNames[agentId] = agent.DisplayName;
            }
        }

        var resources = result.Items.Select(c => new AtsInterviewConfigurationResource
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            AgentId = c.AgentId,
            AgentDisplayName = agentNames.GetValueOrDefault(c.AgentId),
            QuestionCount = c.QuestionCount
        });

        return Ok(resources);
    }

    // Interview Endpoints

    /// <summary>
    /// Creates an interview for an applicant on a job
    /// </summary>
    [HttpPost("interviews")]
    [ProducesResponseType(typeof(InterviewResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<InterviewResource>> CreateInterview([FromBody] CreateInterviewWithApplicantResource resource)
    {
        var organizationId = GetOrganizationId();

        // Get or create job
        var job = await _domainFacade.GetJobByExternalId(organizationId, resource.ExternalJobId);
        if (job == null)
        {
            return BadRequest($"Job with external ID {resource.ExternalJobId} not found. Create the job first.");
        }

        // Get or create applicant
        var applicant = await _domainFacade.GetOrCreateApplicant(
            organizationId,
            resource.ExternalApplicantId,
            resource.ApplicantFirstName,
            resource.ApplicantLastName,
            resource.ApplicantEmail,
            resource.ApplicantPhone);

        // Resolve agent from configuration or direct AgentId
        Guid agentId = resource.AgentId;
        Guid? interviewConfigurationId = resource.InterviewConfigurationId;

        if (interviewConfigurationId.HasValue)
        {
            // Use the configuration to determine agent and questions
            var config = await _domainFacade.GetInterviewConfigurationById(interviewConfigurationId.Value);
            if (config == null || config.OrganizationId != organizationId)
            {
                return BadRequest($"Interview configuration with ID {interviewConfigurationId} not found or does not belong to this organization.");
            }
            agentId = config.AgentId;
        }

        // Validate agent exists and belongs to organization
        var agent = await _domainFacade.GetAgentById(agentId);
        if (agent == null || agent.OrganizationId != organizationId)
        {
            return BadRequest($"Agent with ID {agentId} not found or does not belong to this organization.");
        }

        // Create interview
        var interview = new Interview
        {
            JobId = job.Id,
            ApplicantId = applicant.Id,
            AgentId = agentId,
            InterviewConfigurationId = interviewConfigurationId,
            InterviewType = resource.InterviewType,
            ScheduledAt = resource.ScheduledAt
        };

        var created = await _domainFacade.CreateInterview(interview);

        // Auto-create an invite for the interview so the ATS gets an invite URL
        var invite = await _domainFacade.CreateInterviewInvite(
            created.Id, organizationId, maxUses: 3, expiryDays: 7);

        // Return with interview URL and invite info
        var response = InterviewMapper.ToResource(created);
        var inviteResource = CandidateMapper.ToInviteResource(invite);
        return Created($"/api/v1/ats/interviews/{created.Id}", new
        {
            interview = response,
            invite = inviteResource,
            interviewConfigurationId = interviewConfigurationId,
        });
    }

    /// <summary>
    /// Gets an interview by ID, including current invite status
    /// </summary>
    [HttpGet("interviews/{id}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> GetInterview(Guid id)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }

        var invite = await _domainFacade.GetInterviewInviteByInterviewId(id);
        var inviteStatus = "none";
        if (invite != null)
        {
            if (invite.Status == InviteStatus.Revoked)
                inviteStatus = "revoked";
            else if (invite.ExpiresAt < DateTime.UtcNow)
                inviteStatus = "expired";
            else if (invite.UseCount >= invite.MaxUses)
                inviteStatus = "max_uses_reached";
            else
                inviteStatus = invite.Status; // "active" or "consumed"
        }

        return Ok(new
        {
            interview = InterviewMapper.ToResource(interview),
            inviteStatus,
            inviteUseCount = invite?.UseCount,
            inviteMaxUses = invite?.MaxUses,
            inviteExpiresAt = invite?.ExpiresAt,
        });
    }

    /// <summary>
    /// Creates a new invite for an existing interview, revoking any previous invite.
    /// Use this when the original invite link has expired or reached max uses.
    /// </summary>
    [HttpPost("interviews/{id}/refresh-invite")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> RefreshInvite(Guid id)
    {
        var organizationId = GetOrganizationId();
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }

        // Revoke existing invite if any
        var existingInvite = await _domainFacade.GetInterviewInviteByInterviewId(id);
        if (existingInvite != null && existingInvite.Status == InviteStatus.Active)
        {
            await _domainFacade.RevokeInterviewInvite(existingInvite.Id, "ats_refresh");
        }

        // Create a fresh invite
        var newInvite = await _domainFacade.CreateInterviewInvite(id, organizationId, maxUses: 3, expiryDays: 7);

        var inviteResource = CandidateMapper.ToInviteResource(newInvite);
        var inviteUrl = $"http://localhost:3000/i/{newInvite.ShortCode}";

        return Ok(new
        {
            invite = inviteResource,
            inviteUrl,
        });
    }

    /// <summary>
    /// Gets the result for an interview
    /// </summary>
    [HttpGet("interviews/{id}/result")]
    [ProducesResponseType(typeof(InterviewResultResource), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<InterviewResultResource>> GetInterviewResult(Guid id)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }

        var result = await _domainFacade.GetInterviewResultByInterviewId(id);
        if (result == null)
        {
            return NotFound($"Result for interview {id} not found");
        }

        return Ok(InterviewMapper.ToResultResource(result));
    }
}
