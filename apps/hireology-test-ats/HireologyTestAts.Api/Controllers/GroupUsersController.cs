using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.Auth;
using HireologyTestAts.Api.Mappers;
using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/v1/groups/{groupId:guid}/users")]
[Produces("application/json")]
[Authorize]
public class GroupUsersController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public GroupUsersController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// List all users with access to a group. Requires group admin or superadmin.
    /// </summary>
    [HttpGet]
    [GroupAdminRequired]
    [ProducesResponseType(typeof(GroupUserListResponse), 200)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<GroupUserListResponse>> List(Guid groupId)
    {
        var users = await _domainFacade.GetUsersByGroup(groupId);
        var items = new List<GroupUserResource>();
        foreach (var user in users)
        {
            var isAdmin = await _domainFacade.IsGroupAdmin(user.Id, groupId);
            var orgEntries = await _domainFacade.GetOrganizationAccessEntries(user.Id);
            items.Add(new GroupUserResource
            {
                User = UserMapper.ToResource(user),
                IsAdmin = isAdmin,
                OrganizationAccess = GroupUserMapper.ToResource(orgEntries)
            });
        }
        return Ok(new GroupUserListResponse { Items = items });
    }

    /// <summary>
    /// Invite a user by email to a group with optional organization access.
    /// Creates a pre-provisioned user if they don't exist yet.
    /// Requires group admin or superadmin.
    /// </summary>
    [HttpPost("invite")]
    [GroupAdminRequired]
    [ProducesResponseType(typeof(GroupUserResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<GroupUserResource>> Invite(Guid groupId, [FromBody] InviteUserResource resource)
    {
        if (string.IsNullOrWhiteSpace(resource.Email))
            return BadRequest(new { Message = "Email is required" });

        IReadOnlyList<OrganizationAccessEntry>? orgEntries = null;
        if (resource.OrganizationAccess != null && resource.OrganizationAccess.Count > 0)
        {
            orgEntries = GroupUserMapper.ToDomain(resource.OrganizationAccess);
        }

        var user = await _domainFacade.InviteUserWithOrgAccess(
            resource.Email,
            groupId,
            resource.IsAdmin,
            orgEntries);

        var isAdmin = await _domainFacade.IsGroupAdmin(user.Id, groupId);
        var currentOrgEntries = await _domainFacade.GetOrganizationAccessEntries(user.Id);

        var response = new GroupUserResource
        {
            User = UserMapper.ToResource(user),
            IsAdmin = isAdmin,
            OrganizationAccess = GroupUserMapper.ToResource(currentOrgEntries)
        };

        return StatusCode(201, response);
    }

    /// <summary>
    /// Update a user's organization access within a group.
    /// Requires group admin or superadmin.
    /// </summary>
    [HttpPut("{userId:guid}/access")]
    [GroupAdminRequired]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> SetAccess(Guid groupId, Guid userId, [FromBody] SetGroupUserAccessResource resource)
    {
        var entries = GroupUserMapper.ToDomain(resource.OrganizationAccess);
        await _domainFacade.SetUserOrganizationAccessWithFlags(userId, entries);
        return NoContent();
    }

    /// <summary>
    /// Remove a user from a group.
    /// Requires group admin or superadmin.
    /// </summary>
    [HttpDelete("{userId:guid}")]
    [GroupAdminRequired]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> Remove(Guid groupId, Guid userId)
    {
        await _domainFacade.RemoveUserFromGroup(userId, groupId);
        return NoContent();
    }

    private string? GetAuth0Sub()
    {
        return User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("sub");
    }
}
