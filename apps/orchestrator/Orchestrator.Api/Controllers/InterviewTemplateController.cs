using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;
using Orchestrator.Api.Middleware;

namespace Orchestrator.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class InterviewTemplateController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public InterviewTemplateController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    private UserContext? GetUserContext()
        => HttpContext.Items.TryGetValue("UserContext", out var ctx) ? ctx as UserContext : null;

    [HttpPost]
    [ProducesResponseType(typeof(InterviewTemplateResource), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<InterviewTemplateResource>> Create([FromBody] CreateInterviewTemplateResource resource)
    {
        var userContext = GetUserContext();
        var groupId = userContext?.GroupId ?? resource.GroupId;

        var template = InterviewTemplateMapper.ToDomain(resource, groupId);
        var created = await _domainFacade.CreateInterviewTemplate(template);

        var response = InterviewTemplateMapper.ToResource(created);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InterviewTemplateResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewTemplateResource>> GetById(Guid id)
    {
        var template = await _domainFacade.GetInterviewTemplateById(id);
        if (template == null)
        {
            return NotFound($"Interview template with ID {id} not found");
        }

        if (template.AgentId.HasValue)
        {
            template.Agent = await _domainFacade.GetAgentById(template.AgentId.Value);
        }

        var response = InterviewTemplateMapper.ToResource(template);
        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<InterviewTemplateResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<InterviewTemplateResource>>> Search([FromQuery] SearchInterviewTemplateRequest request)
    {
        var userContext = GetUserContext();
        var groupId = request.GroupId ?? userContext?.GroupId;
        var orgFilter = (userContext is { IsResolved: true, IsSuperadmin: false, IsGroupAdmin: false })
            ? userContext.AccessibleOrganizationIds
            : null;

        var result = await _domainFacade.SearchInterviewTemplates(
            groupId,
            request.AgentId,
            request.Name,
            request.IsActive,
            request.SortBy,
            request.PageNumber,
            request.PageSize,
            orgFilter);

        var response = new PaginatedResponse<InterviewTemplateResource>
        {
            Items = InterviewTemplateMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(InterviewTemplateResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewTemplateResource>> Update(Guid id, [FromBody] UpdateInterviewTemplateResource resource)
    {
        var existing = await _domainFacade.GetInterviewTemplateById(id);
        if (existing == null)
        {
            return NotFound($"Interview template with ID {id} not found");
        }

        var toUpdate = InterviewTemplateMapper.ToDomain(resource, existing);
        var updated = await _domainFacade.UpdateInterviewTemplate(toUpdate);

        var response = InterviewTemplateMapper.ToResource(updated);
        return Ok(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _domainFacade.DeleteInterviewTemplate(id);
        if (!deleted)
        {
            return NotFound($"Interview template with ID {id} not found");
        }

        return NoContent();
    }
}
