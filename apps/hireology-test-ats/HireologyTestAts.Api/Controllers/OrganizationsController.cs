using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.Models;
using HireologyTestAts.Api.Services;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly OrganizationsRepository _organizations;

    public OrganizationsController(OrganizationsRepository organizations)
    {
        _organizations = organizations;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizationItem>), 200)]
    public async Task<ActionResult<IReadOnlyList<OrganizationItem>>> List([FromQuery] Guid? groupId, CancellationToken ct = default)
    {
        var items = await _organizations.ListAsync(groupId, ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrganizationItem), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<OrganizationItem>> GetById(Guid id, CancellationToken ct = default)
    {
        var item = await _organizations.GetByIdAsync(id, ct);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrganizationItem), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<OrganizationItem>> Create([FromBody] CreateOrganizationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required");
        if (request.GroupId == Guid.Empty)
            return BadRequest("GroupId is required");

        var org = new OrganizationItem
        {
            GroupId = request.GroupId,
            Name = request.Name.Trim(),
            City = request.City?.Trim(),
            State = request.State?.Trim()
        };
        var created = await _organizations.CreateAsync(org, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OrganizationItem), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<OrganizationItem>> Update(Guid id, [FromBody] UpdateOrganizationRequest request, CancellationToken ct = default)
    {
        var existing = await _organizations.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();
        if (request.GroupId.HasValue && request.GroupId.Value != Guid.Empty) existing.GroupId = request.GroupId.Value;
        if (!string.IsNullOrWhiteSpace(request.Name)) existing.Name = request.Name.Trim();
        if (request.City != null) existing.City = request.City.Trim();
        if (request.State != null) existing.State = request.State.Trim();
        var updated = await _organizations.UpdateAsync(existing, ct);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await _organizations.DeleteAsync(id, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }
}

public class CreateOrganizationRequest
{
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
}

public class UpdateOrganizationRequest
{
    public Guid? GroupId { get; set; }
    public string? Name { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}
