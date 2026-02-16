using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;
using Orchestrator.Api.Middleware;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for viewing jobs (synced from ATS). Uses JWT; lists jobs for the user's group context.
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

    private UserContext? GetUserContext()
        => HttpContext.Items.TryGetValue("UserContext", out var ctx) ? ctx as UserContext : null;

    /// <summary>
    /// Gets or creates a default group for listing jobs when none is specified
    /// </summary>
    private async Task<Guid> GetOrCreateDefaultGroupAsync()
    {
        const string defaultGroupName = "Default Group";

        var existingGroups = await _domainFacade.SearchGroups(defaultGroupName, true, 1, 1);
        if (existingGroups.Items.Any())
        {
            return existingGroups.Items.First().Id;
        }

        var defaultGroup = new Group
        {
            Name = defaultGroupName,
            IsActive = true
        };
        var created = await _domainFacade.CreateGroup(defaultGroup);
        return created.Id;
    }

    /// <summary>
    /// Searches for jobs (e.g. those synced from an ATS)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<JobResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<JobResource>>> Search([FromQuery] SearchJobRequest request)
    {
        var userContext = GetUserContext();
        var groupId = userContext?.GroupId ?? await GetOrCreateDefaultGroupAsync();
        var orgFilter = (userContext is { IsResolved: true, IsSuperadmin: false, IsGroupAdmin: false })
            ? userContext.AccessibleOrganizationIds
            : null;

        var result = await _domainFacade.SearchJobs(
            groupId,
            request.Title,
            request.Status,
            request.PageNumber,
            request.PageSize,
            orgFilter);

        return Ok(new PaginatedResponse<JobResource>
        {
            Items = JobMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        });
    }
}
