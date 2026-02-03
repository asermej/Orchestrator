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
                Location = resource.Location,
                JobTypeId = resource.JobTypeId
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
            request.JobTypeId,
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

        // Validate agent exists and belongs to organization
        var agent = await _domainFacade.GetAgentById(resource.AgentId);
        if (agent == null || agent.OrganizationId != organizationId)
        {
            return BadRequest($"Agent with ID {resource.AgentId} not found or does not belong to this organization.");
        }

        // Create interview
        var interview = new Interview
        {
            JobId = job.Id,
            ApplicantId = applicant.Id,
            AgentId = resource.AgentId,
            InterviewType = resource.InterviewType,
            ScheduledAt = resource.ScheduledAt
        };

        var created = await _domainFacade.CreateInterview(interview);

        // Return with interview URL
        var response = InterviewMapper.ToResource(created);
        return Created($"/api/v1/ats/interviews/{created.Id}", response);
    }

    /// <summary>
    /// Gets an interview by ID
    /// </summary>
    [HttpGet("interviews/{id}")]
    [ProducesResponseType(typeof(InterviewResource), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<InterviewResource>> GetInterview(Guid id)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }
        return Ok(InterviewMapper.ToResource(interview));
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
