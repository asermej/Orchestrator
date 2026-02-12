using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.Models;
using HireologyTestAts.Api.Services;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly GroupsRepository _groups;

    public GroupsController(GroupsRepository groups)
    {
        _groups = groups;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GroupItem>), 200)]
    public async Task<ActionResult<IReadOnlyList<GroupItem>>> List(CancellationToken ct = default)
    {
        var items = await _groups.ListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GroupItem), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<GroupItem>> GetById(Guid id, CancellationToken ct = default)
    {
        var item = await _groups.GetByIdAsync(id, ct);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GroupItem), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<GroupItem>> Create([FromBody] CreateGroupRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required");

        var group = new GroupItem { Name = request.Name.Trim() };
        var created = await _groups.CreateAsync(group, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GroupItem), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<GroupItem>> Update(Guid id, [FromBody] UpdateGroupRequest request, CancellationToken ct = default)
    {
        var existing = await _groups.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(request.Name)) existing.Name = request.Name.Trim();
        var updated = await _groups.UpdateAsync(existing, ct);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await _groups.DeleteAsync(id, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }
}

public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateGroupRequest
{
    public string? Name { get; set; }
}
