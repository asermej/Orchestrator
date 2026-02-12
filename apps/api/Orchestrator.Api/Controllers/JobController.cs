using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for viewing jobs (synced from ATS). Uses JWT; lists jobs for the default organization.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class JobController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public JobController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Gets or creates a default organization for listing jobs when none is specified
    /// </summary>
    private async Task<Guid> GetOrCreateDefaultOrganizationAsync()
    {
        const string defaultOrgName = "Default Organization";

        var existingOrgs = await _domainFacade.SearchOrganizations(defaultOrgName, true, 1, 1);
        if (existingOrgs.Items.Any())
        {
            return existingOrgs.Items.First().Id;
        }

        var defaultOrg = new Organization
        {
            Name = defaultOrgName,
            IsActive = true
        };
        var created = await _domainFacade.CreateOrganization(defaultOrg);
        return created.Id;
    }

    /// <summary>
    /// Searches for jobs (e.g. those synced from an ATS)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<JobResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<JobResource>>> Search([FromQuery] SearchJobRequest request)
    {
        var organizationId = await GetOrCreateDefaultOrganizationAsync();
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
}
