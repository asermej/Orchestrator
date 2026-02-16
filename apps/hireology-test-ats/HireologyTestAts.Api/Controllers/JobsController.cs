using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.Mappers;
using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public JobsController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    [HttpGet]
    [ProducesResponseType(typeof(JobListResponse), 200)]
    public async Task<ActionResult<JobListResponse>> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? organizationId = null)
    {
        IReadOnlyList<Guid>? orgFilter = null;
        var auth0Sub = GetAuth0Sub();
        if (!string.IsNullOrEmpty(auth0Sub))
        {
            var user = await _domainFacade.GetUserByAuth0Sub(auth0Sub);
            if (user != null)
            {
                var isSuperadmin = user.IsSuperadmin;

                if (organizationId.HasValue)
                {
                    // Explicit org filter: superadmins can access any; others need permission
                    if (!isSuperadmin)
                    {
                        var canAccess = await _domainFacade.CanAccessOrganization(user.Id, organizationId.Value);
                        if (!canAccess) return Forbid();
                    }
                    orgFilter = new[] { organizationId.Value };
                }
                else
                {
                    // No explicit org — use the selected location to scope jobs
                    var selectedId = await _domainFacade.GetSelectedOrganizationId(user.Id);
                    if (selectedId.HasValue)
                    {
                        if (isSuperadmin || await _domainFacade.CanAccessOrganization(user.Id, selectedId.Value))
                        {
                            orgFilter = new[] { selectedId.Value };
                        }
                    }

                    // If no selected location and not superadmin, fall back to all allowed orgs
                    if (orgFilter == null && !isSuperadmin)
                    {
                        var allowedOrgIds = await _domainFacade.GetAllowedOrganizationIds(user.Id);
                        if (allowedOrgIds.Count > 0)
                            orgFilter = allowedOrgIds;
                    }
                    // Superadmin with no selected location: orgFilter stays null → show all jobs
                }
            }
        }
        else if (organizationId.HasValue)
        {
            orgFilter = new[] { organizationId.Value };
        }

        var items = await _domainFacade.GetJobs(pageNumber, pageSize, orgFilter);
        var totalCount = await _domainFacade.GetJobCount(orgFilter);
        var jobIds = items.Select(j => j.Id).ToList();
        var applicantCountByJob = await _domainFacade.GetApplicantCountByJobIds(jobIds);
        var resources = JobMapper.ToResource(items).ToList();
        foreach (var resource in resources)
        {
            resource.ApplicantCount = applicantCountByJob.TryGetValue(resource.Id, out var count) ? count : 0;
        }
        return Ok(new JobListResponse
        {
            Items = resources,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<JobResource>> GetById(Guid id)
    {
        var job = await _domainFacade.GetJobById(id);

        var auth0Sub = GetAuth0Sub();
        if (!string.IsNullOrEmpty(auth0Sub) && job.OrganizationId.HasValue)
        {
            var user = await _domainFacade.GetUserByAuth0Sub(auth0Sub);
            if (user != null)
            {
                var canAccess = await _domainFacade.CanAccessOrganization(user.Id, job.OrganizationId.Value);
                if (!canAccess) return Forbid();
            }
        }

        return Ok(JobMapper.ToResource(job));
    }

    [HttpPost]
    [ProducesResponseType(typeof(JobResource), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<JobResource>> Create([FromBody] CreateJobResource resource)
    {
        var job = JobMapper.ToDomain(resource);

        // Resolve organization from user session if not provided
        var auth0Sub = GetAuth0Sub();
        if (!string.IsNullOrEmpty(auth0Sub))
        {
            var user = await _domainFacade.GetUserByAuth0Sub(auth0Sub);
            if (user != null)
            {
                if (!job.OrganizationId.HasValue)
                {
                    var selectedId = await _domainFacade.GetSelectedOrganizationId(user.Id);
                    job.OrganizationId = selectedId;
                }
                if (job.OrganizationId.HasValue)
                {
                    var canAccess = await _domainFacade.CanAccessOrganization(user.Id, job.OrganizationId.Value);
                    if (!canAccess)
                        throw new AccessDeniedException("You do not have access to the specified organization.");
                }
            }
        }

        var created = await _domainFacade.CreateJob(job);
        var response = JobMapper.ToResource(created);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(JobResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<JobResource>> Update(Guid id, [FromBody] UpdateJobResource resource)
    {
        // Check access on existing job
        var existing = await _domainFacade.GetJobById(id);
        var auth0Sub = GetAuth0Sub();
        if (!string.IsNullOrEmpty(auth0Sub) && existing.OrganizationId.HasValue)
        {
            var user = await _domainFacade.GetUserByAuth0Sub(auth0Sub);
            if (user != null)
            {
                var canAccess = await _domainFacade.CanAccessOrganization(user.Id, existing.OrganizationId.Value);
                if (!canAccess) return Forbid();

                // Also check access on target org if changing
                if (resource.OrganizationId.HasValue)
                {
                    var canAccessTarget = await _domainFacade.CanAccessOrganization(user.Id, resource.OrganizationId.Value);
                    if (!canAccessTarget)
                        throw new AccessDeniedException("You do not have access to the specified organization.");
                }
            }
        }

        var updates = JobMapper.ToDomain(resource);
        var updated = await _domainFacade.UpdateJob(id, updates);
        return Ok(JobMapper.ToResource(updated));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        // Check access
        var existing = await _domainFacade.GetJobById(id);
        var auth0Sub = GetAuth0Sub();
        if (!string.IsNullOrEmpty(auth0Sub) && existing.OrganizationId.HasValue)
        {
            var user = await _domainFacade.GetUserByAuth0Sub(auth0Sub);
            if (user != null)
            {
                var canAccess = await _domainFacade.CanAccessOrganization(user.Id, existing.OrganizationId.Value);
                if (!canAccess) return Forbid();
            }
        }

        await _domainFacade.DeleteJob(id);
        return NoContent();
    }

    private string? GetAuth0Sub()
    {
        return User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("sub");
    }
}
