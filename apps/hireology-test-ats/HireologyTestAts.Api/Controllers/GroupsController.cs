using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.Auth;
using HireologyTestAts.Api.Mappers;
using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public GroupsController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    [HttpGet]
    [SuperadminRequired]
    [ProducesResponseType(typeof(IReadOnlyList<GroupResource>), 200)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<IReadOnlyList<GroupResource>>> List()
    {
        var groups = await _domainFacade.GetGroups(excludeTestData: true);
        return Ok(GroupMapper.ToResource(groups));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GroupResource), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<GroupResource>> GetById(Guid id)
    {
        var auth0Sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("sub");
        if (string.IsNullOrEmpty(auth0Sub)) return Unauthorized();

        var isSuperadmin = await _domainFacade.IsSuperadminByAuth0Sub(auth0Sub);
        if (!isSuperadmin)
        {
            var email = User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");
            var name = User?.FindFirstValue(ClaimTypes.Name) ?? User?.FindFirstValue("name");
            var user = await _domainFacade.GetOrCreateUser(auth0Sub, email, name);
            var isGroupAdmin = await _domainFacade.IsGroupAdmin(user.Id, id);
            if (!isGroupAdmin)
                return StatusCode(403, new { Message = "Superadmin or group admin privileges required" });
        }

        var group = await _domainFacade.GetGroupById(id);
        return Ok(GroupMapper.ToResource(group));
    }

    [HttpPost]
    [SuperadminRequired]
    [ProducesResponseType(typeof(GroupResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<GroupResource>> Create([FromBody] CreateGroupResource resource)
    {
        var group = GroupMapper.ToDomain(resource);
        var created = await _domainFacade.CreateGroup(group, resource.AdminEmail);
        var response = GroupMapper.ToResource(created);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [SuperadminRequired]
    [ProducesResponseType(typeof(GroupResource), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<GroupResource>> Update(Guid id, [FromBody] UpdateGroupResource resource)
    {
        var updates = GroupMapper.ToDomain(resource);
        var updated = await _domainFacade.UpdateGroup(id, updates);
        return Ok(GroupMapper.ToResource(updated));
    }

    [HttpDelete("{id:guid}")]
    [SuperadminRequired]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _domainFacade.DeleteGroup(id);
        return NoContent();
    }
}
