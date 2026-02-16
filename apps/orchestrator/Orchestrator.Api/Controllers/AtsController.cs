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

    private Guid GetGroupId()
    {
        if (HttpContext.Items.TryGetValue("GroupId", out var groupId) && groupId is Guid id)
        {
            return id;
        }
        throw new UnauthorizedAccessException("Group not found in context");
    }

    // Group Sync Endpoints

    /// <summary>
    /// Creates or updates a group by its external (ATS) group ID.
    /// Authenticated via X-API-Key (existing group) or X-Bootstrap-Secret (first-time setup).
    /// Returns the Orchestrator group including the API key the ATS should store.
    /// </summary>
    [HttpPost("groups")]
    [ProducesResponseType(typeof(SyncGroupResponseResource), 200)]
    [ProducesResponseType(typeof(SyncGroupResponseResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<SyncGroupResponseResource>> SyncGroup([FromBody] SyncGroupResource resource)
    {
        if (resource.ExternalGroupId == Guid.Empty)
        {
            return BadRequest(new { error = "ExternalGroupId is required" });
        }

        if (string.IsNullOrWhiteSpace(resource.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        var existingGroup = await _domainFacade.GetGroupByExternalGroupId(resource.ExternalGroupId);
        var group = await _domainFacade.UpsertGroupByExternalId(
            resource.ExternalGroupId, resource.Name.Trim(), resource.AtsBaseUrl);

        var response = new SyncGroupResponseResource
        {
            Id = group.Id,
            Name = group.Name,
            ApiKey = group.ApiKey,
            ExternalGroupId = group.ExternalGroupId!.Value,
            IsNew = existingGroup == null
        };

        if (existingGroup == null)
        {
            return Created($"/api/v1/ats/groups/{group.Id}", response);
        }
        return Ok(response);
    }

    // Settings Endpoints

    /// <summary>
    /// Gets the current webhook URL for the authenticated group
    /// </summary>
    [HttpGet("settings/webhook")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> GetWebhookUrl()
    {
        var groupId = GetGroupId();
        var group = await _domainFacade.GetGroupById(groupId);
        if (group == null)
        {
            return NotFound("Group not found");
        }

        return Ok(new
        {
            webhookUrl = group.WebhookUrl,
            configured = !string.IsNullOrEmpty(group.WebhookUrl)
        });
    }

    /// <summary>
    /// Updates the webhook URL for the authenticated group
    /// </summary>
    [HttpPut("settings/webhook")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> UpdateWebhookUrl([FromBody] UpdateWebhookResource resource)
    {
        var groupId = GetGroupId();
        var group = await _domainFacade.GetGroupById(groupId);
        if (group == null)
        {
            return NotFound("Group not found");
        }

        group.WebhookUrl = resource.WebhookUrl;
        await _domainFacade.UpdateGroup(group);

        return Ok(new { webhookUrl = group.WebhookUrl });
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
        var groupId = GetGroupId();

        // Check if job already exists
        var existing = await _domainFacade.GetJobByExternalId(groupId, resource.ExternalJobId);
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
            if (resource.OrganizationId.HasValue)
                updatedJob.OrganizationId = resource.OrganizationId;
            var updated = await _domainFacade.UpdateJob(updatedJob);
            return Ok(JobMapper.ToResource(updated));
        }

        // Create new job
        var job = JobMapper.ToDomain(resource, groupId);
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
        var groupId = GetGroupId();
        var job = await _domainFacade.GetJobByExternalId(groupId, externalJobId);
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
        var groupId = GetGroupId();
        var result = await _domainFacade.SearchJobs(
            groupId,
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
        var groupId = GetGroupId();
        var job = await _domainFacade.GetJobByExternalId(groupId, externalJobId);
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
        var groupId = GetGroupId();

        // Check if applicant already exists
        var existing = await _domainFacade.GetApplicantByExternalId(groupId, resource.ExternalApplicantId);
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
            if (resource.OrganizationId.HasValue)
                updatedApplicant.OrganizationId = resource.OrganizationId;
            var updated = await _domainFacade.UpdateApplicant(updatedApplicant);
            return Ok(ApplicantMapper.ToResource(updated));
        }

        // Create new applicant
        var applicant = ApplicantMapper.ToDomain(resource, groupId);
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
        var groupId = GetGroupId();
        var applicant = await _domainFacade.GetApplicantByExternalId(groupId, externalApplicantId);
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
        var groupId = GetGroupId();
        var result = await _domainFacade.SearchApplicants(
            groupId,
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
    /// Lists agents available for the authenticated group
    /// </summary>
    [HttpGet("agents")]
    [ProducesResponseType(typeof(IEnumerable<AtsAgentResource>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<IEnumerable<AtsAgentResource>>> ListAgents()
    {
        var groupId = GetGroupId();
        var result = await _domainFacade.SearchAgents(
            groupId, null, null, null, 1, 100);

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
    /// Lists interview configurations available for the authenticated group.
    /// Each configuration defines the agent, questions, and scoring rubric for an interview.
    /// </summary>
    [HttpGet("configurations")]
    [ProducesResponseType(typeof(IEnumerable<AtsInterviewConfigurationResource>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<IEnumerable<AtsInterviewConfigurationResource>>> ListConfigurations()
    {
        var groupId = GetGroupId();
        var result = await _domainFacade.SearchInterviewConfigurations(
            groupId, null, null, true, null, 1, 100);

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
        var groupId = GetGroupId();

        // Get or create job
        var job = await _domainFacade.GetJobByExternalId(groupId, resource.ExternalJobId);
        if (job == null)
        {
            return BadRequest($"Job with external ID {resource.ExternalJobId} not found. Create the job first.");
        }

        // Get or create applicant
        var applicant = await _domainFacade.GetOrCreateApplicant(
            groupId,
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
            if (config == null || config.GroupId != groupId)
            {
                return BadRequest($"Interview configuration with ID {interviewConfigurationId} not found or does not belong to this group.");
            }
            agentId = config.AgentId;
        }

        // Validate agent exists and belongs to group
        var agent = await _domainFacade.GetAgentById(agentId);
        if (agent == null || agent.GroupId != groupId)
        {
            return BadRequest($"Agent with ID {agentId} not found or does not belong to this group.");
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
            created.Id, groupId, maxUses: 3, expiryDays: 7);

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
        var groupId = GetGroupId();
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
        var newInvite = await _domainFacade.CreateInterviewInvite(id, groupId, maxUses: 3, expiryDays: 7);

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

    /// <summary>
    /// Gets the responses for an interview (questions, transcripts, audio URLs)
    /// </summary>
    [HttpGet("interviews/{id}/responses")]
    [ProducesResponseType(typeof(IReadOnlyList<InterviewResponseResource>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<IReadOnlyList<InterviewResponseResource>>> GetInterviewResponses(Guid id)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }

        var responses = await _domainFacade.GetInterviewResponsesByInterviewId(id);
        return Ok(responses.Select(InterviewMapper.ToResponseResource).ToList());
    }

}
