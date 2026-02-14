using System.Security.Claims;
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
public class ApplicantsController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public ApplicantsController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Lists applicants scoped to the user's accessible organizations.
    /// </summary>
    [HttpGet("applicants")]
    [ProducesResponseType(typeof(ApplicantListResponse), 200)]
    public async Task<ActionResult<ApplicantListResponse>> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        IReadOnlyList<Guid>? orgFilter = null;
        var auth0Sub = GetAuth0Sub();
        if (!string.IsNullOrEmpty(auth0Sub))
        {
            var user = await _domainFacade.GetUserByAuth0Sub(auth0Sub);
            if (user != null)
            {
                if (!user.IsSuperadmin)
                {
                    var selectedId = await _domainFacade.GetSelectedOrganizationId(user.Id);
                    if (selectedId.HasValue)
                    {
                        orgFilter = new[] { selectedId.Value };
                    }
                    else
                    {
                        var allowedOrgIds = await _domainFacade.GetAllowedOrganizationIds(user.Id);
                        if (allowedOrgIds.Count > 0) orgFilter = allowedOrgIds;
                    }
                }
            }
        }

        var items = await _domainFacade.GetApplicants(pageNumber, pageSize, orgFilter);
        var totalCount = await _domainFacade.GetApplicantCount(orgFilter);
        return Ok(new ApplicantListResponse
        {
            Items = ApplicantMapper.ToResource(items),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Gets applicants for a specific job.
    /// </summary>
    [HttpGet("jobs/{jobId:guid}/applicants")]
    [ProducesResponseType(typeof(IReadOnlyList<ApplicantResource>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IReadOnlyList<ApplicantResource>>> GetByJob(Guid jobId)
    {
        try
        {
            await _domainFacade.GetJobById(jobId);
        }
        catch (JobNotFoundException)
        {
            return NotFound("Job not found");
        }

        var applicants = await _domainFacade.GetApplicantsByJobId(jobId);
        return Ok(ApplicantMapper.ToResource(applicants));
    }

    /// <summary>
    /// Apply for a job. Creates an applicant record linked to the job.
    /// </summary>
    [HttpPost("jobs/{jobId:guid}/apply")]
    [ProducesResponseType(typeof(ApplicantResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ApplicantResource>> Apply(Guid jobId, [FromBody] CreateApplicantResource resource)
    {
        // Verify job exists
        Job job;
        try
        {
            job = await _domainFacade.GetJobById(jobId);
        }
        catch (JobNotFoundException)
        {
            return NotFound("Job not found");
        }

        var applicant = ApplicantMapper.ToDomain(resource, jobId, job.OrganizationId);
        var created = await _domainFacade.CreateApplicant(applicant);
        return Created($"/api/v1/applicants/{created.Id}", ApplicantMapper.ToResource(created));
    }

    private string? GetAuth0Sub()
    {
        return User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("sub");
    }
}
