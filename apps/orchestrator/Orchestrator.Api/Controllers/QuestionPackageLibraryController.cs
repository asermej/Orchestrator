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
public class QuestionPackageLibraryController : ControllerBase
{
    private readonly DomainFacade _domainFacade;
    private readonly ILogger<QuestionPackageLibraryController> _logger;

    public QuestionPackageLibraryController(DomainFacade domainFacade, ILogger<QuestionPackageLibraryController> logger)
    {
        _domainFacade = domainFacade;
        _logger = logger;
    }

    private UserContext? GetUserContext()
        => HttpContext.Items.TryGetValue("UserContext", out var ctx) ? ctx as UserContext : null;

    private Guid? GetSelectedOrganizationId()
    {
        if (HttpContext.Request.Headers.TryGetValue("X-Organization-Id", out var orgIdHeader)
            && Guid.TryParse(orgIdHeader.FirstOrDefault(), out var orgId))
        {
            return orgId;
        }
        return null;
    }

    private List<Guid> GetAncestorOrgIds(Guid currentOrgId, UserContext userContext)
    {
        var ancestors = new List<Guid>();
        var orgMap = userContext.AccessibleOrganizations.ToDictionary(o => o.Id);
        var currentId = currentOrgId;
        while (orgMap.TryGetValue(currentId, out var org) && org.ParentOrganizationId.HasValue)
        {
            ancestors.Add(org.ParentOrganizationId.Value);
            currentId = org.ParentOrganizationId.Value;
        }
        return ancestors;
    }

    private Dictionary<Guid, string> BuildOrgNameLookup(UserContext userContext)
    {
        return userContext.AccessibleOrganizations.ToDictionary(o => o.Id, o => o.Name);
    }

    // ==================== READ ENDPOINTS ====================

    [HttpGet("universal-rubric")]
    [ProducesResponseType(typeof(List<UniversalRubricLevelResource>), 200)]
    public ActionResult<List<UniversalRubricLevelResource>> GetUniversalRubric()
    {
        var levels = UniversalRubric.GetAllLevels();
        return Ok(QuestionPackageLibraryMapper.ToResource(levels));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<RoleTemplateResource>), 200)]
    public async Task<ActionResult<List<RoleTemplateResource>>> GetAll(
        [FromQuery] string? source = null,
        [FromQuery] Guid? groupId = null)
    {
        var userContext = GetUserContext();
        var effectiveGroupId = groupId ?? userContext?.GroupId;
        var selectedOrgId = GetSelectedOrganizationId();

        if (source is "local" or "inherited" or "system")
        {
            if (source == "system")
            {
                var systemRoles = await _domainFacade.SearchSystemRoleTemplatesAsync();
                return Ok(QuestionPackageLibraryMapper.ToResource(systemRoles, isInherited: false).ToList());
            }

            if (!selectedOrgId.HasValue || !effectiveGroupId.HasValue)
                return BadRequest(new ErrorResponse { Message = "An organization must be selected to filter by local or inherited." });

            if (source == "local")
            {
                var localRoles = await _domainFacade.SearchLocalRoleTemplatesAsync(effectiveGroupId.Value, selectedOrgId.Value);
                return Ok(QuestionPackageLibraryMapper.ToResource(localRoles, isInherited: false).ToList());
            }

            // inherited
            var ancestorOrgIds = GetAncestorOrgIds(selectedOrgId.Value, userContext!);
            var inheritedRoles = await _domainFacade.SearchInheritedRoleTemplatesAsync(effectiveGroupId.Value, ancestorOrgIds);
            var orgNameLookup = BuildOrgNameLookup(userContext!);
            return Ok(QuestionPackageLibraryMapper.ToResource(inheritedRoles, isInherited: true, orgNameLookup).ToList());
        }

        var roleTemplates = await _domainFacade.GetRoleTemplatesByFilterAsync(source, effectiveGroupId);
        var response = QuestionPackageLibraryMapper.ToResource(roleTemplates).ToList();
        return Ok(response);
    }

    [HttpGet("{roleKey}")]
    [ProducesResponseType(typeof(RoleTemplateDetailResource), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<RoleTemplateDetailResource>> GetByRoleKey(string roleKey)
    {
        var roleTemplate = await _domainFacade.GetRoleTemplateWithFullDetailsAsync(roleKey);
        if (roleTemplate == null)
            return NotFound(new ErrorResponse { Message = $"Role template with key '{roleKey}' not found" });

        return Ok(QuestionPackageLibraryMapper.ToDetailResource(roleTemplate));
    }

    // ==================== ROLE TEMPLATE CRUD ====================

    [HttpPost]
    [ProducesResponseType(typeof(RoleTemplateResource), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<RoleTemplateResource>> CreateRoleTemplate([FromBody] CreateRoleTemplateRequest request)
    {
        var userContext = GetUserContext();
        var groupId = userContext?.GroupId ?? Guid.Empty;
        var selectedOrgId = GetSelectedOrganizationId();

        if (request.OrganizationId == null && selectedOrgId.HasValue)
            request.OrganizationId = selectedOrgId.Value;

        var roleTemplate = QuestionPackageLibraryMapper.ToDomain(request, groupId);
        var created = await _domainFacade.CreateRoleTemplateAsync(roleTemplate);

        return CreatedAtAction(nameof(GetByRoleKey), new { roleKey = created.RoleKey },
            QuestionPackageLibraryMapper.ToResource(created));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(RoleTemplateResource), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<RoleTemplateResource>> UpdateRoleTemplate(Guid id, [FromBody] UpdateRoleTemplateRequest request)
    {
        var existing = await _domainFacade.GetRoleTemplateByIdAsync(id);
        if (existing == null)
            return NotFound(new ErrorResponse { Message = $"Role template {id} not found" });

        QuestionPackageLibraryMapper.ApplyUpdate(request, existing);
        var updated = await _domainFacade.UpdateRoleTemplateAsync(existing);

        return Ok(QuestionPackageLibraryMapper.ToResource(updated));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> DeleteRoleTemplate(Guid id)
    {
        var userContext = GetUserContext();
        await _domainFacade.DeleteRoleTemplateAsync(id, userContext?.Auth0Sub);
        return NoContent();
    }

    [HttpPost("{id}/clone")]
    [ProducesResponseType(typeof(RoleTemplateDetailResource), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<RoleTemplateDetailResource>> Clone(Guid id)
    {
        var userContext = GetUserContext();
        var selectedOrgId = GetSelectedOrganizationId();
        var groupId = userContext?.GroupId;

        if (!selectedOrgId.HasValue)
            return BadRequest(new ErrorResponse { Message = "An organization must be selected to clone a role." });

        if (!groupId.HasValue)
            return BadRequest(new ErrorResponse { Message = "Group context is required." });

        var cloned = await _domainFacade.CloneRoleTemplateAsync(id, selectedOrgId.Value, groupId.Value, userContext?.Auth0Sub);
        return CreatedAtAction(nameof(GetByRoleKey), new { roleKey = cloned.RoleKey },
            QuestionPackageLibraryMapper.ToDetailResource(cloned));
    }

    // ==================== COMPETENCY CRUD ====================

    [HttpPost("{roleTemplateId}/competencies")]
    [ProducesResponseType(typeof(CompetencyResource), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<CompetencyResource>> CreateCompetency(Guid roleTemplateId, [FromBody] CreateCompetencyRequest request)
    {
        var competency = QuestionPackageLibraryMapper.ToDomain(request, roleTemplateId);
        var created = await _domainFacade.CreateCompetencyAsync(competency);
        return StatusCode(201, QuestionPackageLibraryMapper.ToResource(created));
    }

    [HttpPut("competencies/{competencyId}")]
    [ProducesResponseType(typeof(CompetencyResource), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<CompetencyResource>> UpdateCompetency(Guid competencyId, [FromBody] UpdateCompetencyRequest request, [FromQuery] Guid roleTemplateId)
    {
        var competency = new Competency
        {
            Id = competencyId,
            RoleTemplateId = roleTemplateId,
            Name = request.Name,
            Description = request.Description,
            CanonicalExample = request.CanonicalExample,
            DefaultWeight = request.DefaultWeight,
            IsRequired = request.IsRequired,
            DisplayOrder = request.DisplayOrder
        };
        var updated = await _domainFacade.UpdateCompetencyAsync(competency);
        return Ok(QuestionPackageLibraryMapper.ToResource(updated));
    }

    [HttpDelete("competencies/{competencyId}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteCompetency(Guid competencyId, [FromQuery] Guid roleTemplateId)
    {
        var userContext = GetUserContext();
        await _domainFacade.DeleteCompetencyAsync(competencyId, roleTemplateId, userContext?.Auth0Sub);
        return NoContent();
    }

    // ==================== AI GENERATION ENDPOINTS ====================

    [HttpPost("ai/suggest-competencies")]
    [ProducesResponseType(typeof(List<AISuggestedCompetencyResource>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<List<AISuggestedCompetencyResource>>> SuggestCompetencies([FromBody] AISuggestCompetenciesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName) || string.IsNullOrWhiteSpace(request.Industry))
            return BadRequest(new ErrorResponse { Message = "RoleName and Industry are required." });

        var suggestions = await _domainFacade.GenerateCompetencySuggestionsAsync(request.RoleName, request.Industry);
        return Ok(QuestionPackageLibraryMapper.ToResource(suggestions));
    }

    [HttpPost("ai/suggest-canonical-example")]
    [ProducesResponseType(typeof(AISuggestCanonicalExampleResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<AISuggestCanonicalExampleResponse>> SuggestCanonicalExample([FromBody] AISuggestCanonicalExampleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompetencyName))
            return BadRequest(new ErrorResponse { Message = "CompetencyName is required." });
        if (string.IsNullOrWhiteSpace(request.RoleContext))
            return BadRequest(new ErrorResponse { Message = "RoleContext is required." });

        var suggested = await _domainFacade.GenerateCanonicalExampleAsync(request.CompetencyName, request.Description, request.RoleContext);
        return Ok(new AISuggestCanonicalExampleResponse { SuggestedExample = suggested });
    }
}
