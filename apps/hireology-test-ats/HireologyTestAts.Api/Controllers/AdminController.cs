using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HireologyTestAts.Api.Auth;
using HireologyTestAts.Api.Mappers;
using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Produces("application/json")]
[Authorize]
[SuperadminRequired]
public class AdminController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public AdminController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// List all superadmins.
    /// </summary>
    [HttpGet("superadmins")]
    [ProducesResponseType(typeof(IReadOnlyList<SuperadminResource>), 200)]
    public async Task<ActionResult<IReadOnlyList<SuperadminResource>>> ListSuperadmins()
    {
        var superadmins = await _domainFacade.GetSuperadmins();
        return Ok(AdminMapper.ToSuperadminResource(superadmins));
    }

    /// <summary>
    /// Promote a user to superadmin.
    /// </summary>
    [HttpPost("superadmins")]
    [ProducesResponseType(typeof(SuperadminResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<SuperadminResource>> PromoteSuperadmin([FromBody] PromoteSuperadminResource resource)
    {
        var requestingUser = await GetCurrentUser();
        if (requestingUser == null) return Unauthorized();

        var promoted = await _domainFacade.SetSuperadmin(requestingUser.Id, resource.UserId, true);
        return CreatedAtAction(nameof(ListSuperadmins), AdminMapper.ToSuperadminResource(promoted));
    }

    /// <summary>
    /// Demote a superadmin (remove superadmin privileges).
    /// </summary>
    [HttpDelete("superadmins/{userId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> DemoteSuperadmin(Guid userId)
    {
        var requestingUser = await GetCurrentUser();
        if (requestingUser == null) return Unauthorized();

        await _domainFacade.SetSuperadmin(requestingUser.Id, userId, false);
        return NoContent();
    }

    private async Task<User?> GetCurrentUser()
    {
        var auth0Sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("sub");
        if (string.IsNullOrEmpty(auth0Sub)) return null;
        return await _domainFacade.GetUserByAuth0Sub(auth0Sub);
    }
}
