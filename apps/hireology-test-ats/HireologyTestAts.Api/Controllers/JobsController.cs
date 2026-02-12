using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.Models;
using HireologyTestAts.Api.Services;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly JobsRepository _jobs;
    private readonly OrchestratorSyncService _orchestrator;
    private readonly ICurrentUserService _currentUser;
    private readonly UserAccessService _userAccess;
    private readonly UserSessionsRepository _sessions;

    public JobsController(JobsRepository jobs, OrchestratorSyncService orchestrator, ICurrentUserService currentUser, UserAccessService userAccess, UserSessionsRepository sessions)
    {
        _jobs = jobs;
        _orchestrator = orchestrator;
        _currentUser = currentUser;
        _userAccess = userAccess;
        _sessions = sessions;
    }

    [HttpGet]
    [ProducesResponseType(typeof(JobListResponse), 200)]
    public async Task<ActionResult<JobListResponse>> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? organizationId = null, CancellationToken ct = default)
    {
        IReadOnlyList<Guid>? orgFilter = null;
        var user = await _currentUser.GetCurrentUserAsync(ct);
        if (user != null)
        {
            var allowedOrgIds = await _userAccess.GetAllowedOrganizationIdsAsync(user.Id, ct);
            if (organizationId.HasValue)
            {
                if (!allowedOrgIds.Contains(organizationId.Value))
                    return Forbid();
                orgFilter = new[] { organizationId.Value };
            }
            else
            {
                var selectedId = await _sessions.GetSelectedOrganizationIdAsync(user.Id, ct);
                if (selectedId.HasValue && allowedOrgIds.Contains(selectedId.Value))
                    orgFilter = new[] { selectedId.Value };
                else if (allowedOrgIds.Count > 0)
                    orgFilter = allowedOrgIds;
            }
        }
        else if (organizationId.HasValue)
            orgFilter = new[] { organizationId.Value };

        var items = await _jobs.ListAsync(pageNumber, pageSize, orgFilter, ct);
        var totalCount = await _jobs.CountAsync(orgFilter, ct);
        return Ok(new JobListResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobItem), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<JobItem>> GetById(Guid id, CancellationToken ct = default)
    {
        var job = await _jobs.GetByIdAsync(id, ct);
        if (job == null) return NotFound();
        var user = await _currentUser.GetCurrentUserAsync(ct);
        if (user != null && job.OrganizationId.HasValue)
        {
            var canAccess = await _userAccess.CanAccessOrganizationAsync(user.Id, job.OrganizationId.Value, ct);
            if (!canAccess) return Forbid();
        }
        return Ok(job);
    }

    [HttpPost]
    [ProducesResponseType(typeof(JobItem), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<JobItem>> Create([FromBody] CreateJobRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalJobId) || string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("ExternalJobId and Title are required");

        var organizationId = request.OrganizationId;
        var user = await _currentUser.GetCurrentUserAsync(ct);
        if (user != null)
        {
            if (!organizationId.HasValue)
            {
                var selectedId = await _sessions.GetSelectedOrganizationIdAsync(user.Id, ct);
                organizationId = selectedId;
            }
            if (organizationId.HasValue)
            {
                var canAccess = await _userAccess.CanAccessOrganizationAsync(user.Id, organizationId.Value, ct);
                if (!canAccess) return BadRequest("You do not have access to the specified organization.");
            }
        }

        var existing = await _jobs.GetByExternalIdAsync(request.ExternalJobId, ct);
        if (existing != null)
            return BadRequest($"Job with ExternalJobId '{request.ExternalJobId}' already exists");

        var job = new JobItem
        {
            ExternalJobId = request.ExternalJobId.Trim(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Location = request.Location?.Trim(),
            Status = request.Status ?? "active",
            OrganizationId = organizationId
        };
        var created = await _jobs.CreateAsync(job, ct);

        await _orchestrator.SyncJobToOrchestratorAsync(created, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(JobItem), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<JobItem>> Update(Guid id, [FromBody] UpdateJobRequest request, CancellationToken ct = default)
    {
        var existing = await _jobs.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();

        var user = await _currentUser.GetCurrentUserAsync(ct);
        if (user != null && existing.OrganizationId.HasValue)
        {
            var canAccess = await _userAccess.CanAccessOrganizationAsync(user.Id, existing.OrganizationId.Value, ct);
            if (!canAccess) return Forbid();
        }

        existing.Title = request.Title ?? existing.Title;
        existing.Description = request.Description ?? existing.Description;
        existing.Location = request.Location ?? existing.Location;
        existing.Status = request.Status ?? existing.Status;
        if (request.OrganizationId.HasValue)
        {
            if (user != null)
            {
                var canAccess = await _userAccess.CanAccessOrganizationAsync(user.Id, request.OrganizationId.Value, ct);
                if (!canAccess) return BadRequest("You do not have access to the specified organization.");
            }
            existing.OrganizationId = request.OrganizationId;
        }

        var updated = await _jobs.UpdateAsync(existing, ct);
        if (updated == null) return NotFound();

        await _orchestrator.SyncJobToOrchestratorAsync(updated, ct);

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var existing = await _jobs.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();

        var user = await _currentUser.GetCurrentUserAsync(ct);
        if (user != null && existing.OrganizationId.HasValue)
        {
            var canAccess = await _userAccess.CanAccessOrganizationAsync(user.Id, existing.OrganizationId.Value, ct);
            if (!canAccess) return Forbid();
        }

        var externalId = existing.ExternalJobId;
        var deleted = await _jobs.DeleteAsync(id, ct);
        if (!deleted) return NotFound();

        await _orchestrator.DeleteJobFromOrchestratorAsync(externalId, ct);

        return NoContent();
    }
}

public class JobListResponse
{
    public IReadOnlyList<JobItem> Items { get; set; } = Array.Empty<JobItem>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class CreateJobRequest
{
    public string ExternalJobId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public Guid? OrganizationId { get; set; }
}

public class UpdateJobRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public Guid? OrganizationId { get; set; }
}
