using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.Models;
using HireologyTestAts.Api.Services;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UsersRepository _users;
    private readonly UserAccessRepository _userAccess;

    public UsersController(UsersRepository users, UserAccessRepository userAccess)
    {
        _users = users;
        _userAccess = userAccess;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserListResponse), 200)]
    public async Task<ActionResult<UserListResponse>> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var items = await _users.ListAsync(pageNumber, pageSize, ct);
        var totalCount = await _users.CountAsync(ct);
        return Ok(new UserListResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserWithAccessResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserWithAccessResponse>> GetById(Guid id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user == null) return NotFound();

        var groupIds = await _userAccess.GetGroupIdsAsync(id, ct);
        var organizationIds = await _userAccess.GetOrganizationIdsAsync(id, ct);

        return Ok(new UserWithAccessResponse
        {
            User = user,
            GroupIds = groupIds,
            OrganizationIds = organizationIds
        });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserItem), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserItem>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct = default)
    {
        var existing = await _users.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();

        if (request.Email != null) existing.Email = request.Email.Trim();
        if (request.Name != null) existing.Name = request.Name.Trim();

        var updated = await _users.UpdateAsync(existing, ct);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpPut("{id:guid}/access")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> SetAccess(Guid id, [FromBody] SetUserAccessRequest request, CancellationToken ct = default)
    {
        var existing = await _users.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();

        await _userAccess.SetGroupAccessAsync(id, request.GroupIds ?? Array.Empty<Guid>(), ct);
        await _userAccess.SetOrganizationAccessAsync(id, request.OrganizationIds ?? Array.Empty<Guid>(), ct);
        return NoContent();
    }
}

public class UserListResponse
{
    public IReadOnlyList<UserItem> Items { get; set; } = Array.Empty<UserItem>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class UserWithAccessResponse
{
    public UserItem User { get; set; } = null!;
    public IReadOnlyList<Guid> GroupIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<Guid> OrganizationIds { get; set; } = Array.Empty<Guid>();
}

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? Name { get; set; }
}

public class SetUserAccessRequest
{
    public IReadOnlyList<Guid>? GroupIds { get; set; }
    public IReadOnlyList<Guid>? OrganizationIds { get; set; }
}
