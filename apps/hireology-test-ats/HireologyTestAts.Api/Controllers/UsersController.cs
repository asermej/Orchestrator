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
public class UsersController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public UsersController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserListResponse), 200)]
    public async Task<ActionResult<UserListResponse>> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var items = await _domainFacade.GetUsers(pageNumber, pageSize);
        var totalCount = await _domainFacade.GetUserCount();
        return Ok(new UserListResponse
        {
            Items = UserMapper.ToResource(items),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserWithAccessResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserWithAccessResponse>> GetById(Guid id)
    {
        var user = await _domainFacade.GetUserById(id);
        var groupIds = await _domainFacade.GetUserGroupIds(id);
        var organizationIds = await _domainFacade.GetUserOrganizationIds(id);

        return Ok(new UserWithAccessResponse
        {
            User = UserMapper.ToResource(user),
            GroupIds = groupIds,
            OrganizationIds = organizationIds
        });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserResource>> Update(Guid id, [FromBody] UpdateUserResource resource)
    {
        var updates = UserMapper.ToDomain(resource);
        var updated = await _domainFacade.UpdateUser(id, updates);
        return Ok(UserMapper.ToResource(updated));
    }

    [HttpPut("{id:guid}/access")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> SetAccess(Guid id, [FromBody] SetUserAccessResource resource)
    {
        await _domainFacade.SetUserAccess(id, resource.GroupIds, resource.OrganizationIds);
        return NoContent();
    }
}
