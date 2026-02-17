using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;
using Orchestrator.Api.Middleware;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for Interview Guide management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class InterviewGuideController : ControllerBase
{
    private readonly DomainFacade _domainFacade;
    private readonly ILogger<InterviewGuideController> _logger;

    public InterviewGuideController(DomainFacade domainFacade, ILogger<InterviewGuideController> logger)
    {
        _domainFacade = domainFacade;
        _logger = logger;
    }

    private UserContext? GetUserContext()
        => HttpContext.Items.TryGetValue("UserContext", out var ctx) ? ctx as UserContext : null;

    /// <summary>
    /// Reads the X-Organization-Id header to determine the currently selected organization.
    /// </summary>
    private Guid? GetSelectedOrganizationId()
    {
        if (HttpContext.Request.Headers.TryGetValue("X-Organization-Id", out var orgIdHeader)
            && Guid.TryParse(orgIdHeader.FirstOrDefault(), out var orgId))
        {
            return orgId;
        }
        return null;
    }

    /// <summary>
    /// Computes the ancestor organization IDs for the given org by walking up the parent chain.
    /// </summary>
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

    /// <summary>
    /// Builds a lookup of organization ID to name from the user context.
    /// </summary>
    private Dictionary<Guid, string> BuildOrgNameLookup(UserContext userContext)
    {
        return userContext.AccessibleOrganizations.ToDictionary(o => o.Id, o => o.Name);
    }

    /// <summary>
    /// Creates a new interview guide
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InterviewGuideResource), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<InterviewGuideResource>> Create([FromBody] CreateInterviewGuideResource resource)
    {
        _logger.LogInformation("Creating InterviewGuide: {@Resource}", resource);

        var userContext = GetUserContext();
        var groupId = userContext?.GroupId ?? resource.GroupId;

        // Set organization ID from header if not explicitly provided in the request body
        if (!resource.OrganizationId.HasValue)
        {
            resource.OrganizationId = GetSelectedOrganizationId();
        }

        var guide = InterviewGuideMapper.ToDomain(resource, groupId);
        var createdGuide = await _domainFacade.CreateInterviewGuide(guide);

        var response = InterviewGuideMapper.ToResource(createdGuide);
        _logger.LogInformation("Successfully created InterviewGuide with ID: {Id}", response.Id);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets an interview guide by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InterviewGuideResource), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<InterviewGuideResource>> GetById(Guid id, [FromQuery] bool includeQuestions = true)
    {
        _logger.LogInformation("Getting InterviewGuide by ID: {Id}", id);

        InterviewGuide? guide;

        if (includeQuestions)
        {
            guide = await _domainFacade.GetInterviewGuideByIdWithQuestions(id);
        }
        else
        {
            guide = await _domainFacade.GetInterviewGuideById(id);
        }

        if (guide == null)
        {
            return NotFound(new ErrorResponse { Message = $"Interview guide with ID {id} not found" });
        }

        var response = InterviewGuideMapper.ToResource(guide);
        _logger.LogInformation("Successfully retrieved InterviewGuide with ID: {Id}", id);

        return Ok(response);
    }

    /// <summary>
    /// Searches for interview guides with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<InterviewGuideResource>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<PaginatedResponse<InterviewGuideResource>>> Search([FromQuery] SearchInterviewGuideRequest request)
    {
        _logger.LogInformation("Searching InterviewGuides with criteria: {@Criteria}", request);

        var userContext = GetUserContext();
        var groupId = request.GroupId ?? userContext?.GroupId;
        var selectedOrgId = GetSelectedOrganizationId();

        // When Source is specified (local/inherited), require an organization to be selected
        if (!string.IsNullOrWhiteSpace(request.Source) && !selectedOrgId.HasValue)
        {
            return BadRequest("An organization must be selected to filter by source.");
        }

        // Handle local/inherited source filtering
        if (!string.IsNullOrWhiteSpace(request.Source) && selectedOrgId.HasValue && groupId.HasValue && userContext != null)
        {
            var orgNameLookup = BuildOrgNameLookup(userContext);

            if (request.Source.Equals("local", StringComparison.OrdinalIgnoreCase))
            {
                var localResult = await _domainFacade.SearchLocalInterviewGuides(
                    groupId.Value,
                    selectedOrgId.Value,
                    request.Name,
                    request.IsActive,
                    request.SortBy,
                    request.PageNumber,
                    request.PageSize);

                return Ok(new PaginatedResponse<InterviewGuideResource>
                {
                    Items = InterviewGuideMapper.ToResource(localResult.Items, isInherited: false, orgNameLookup: orgNameLookup),
                    TotalCount = localResult.TotalCount,
                    PageNumber = localResult.PageNumber,
                    PageSize = localResult.PageSize
                });
            }

            if (request.Source.Equals("inherited", StringComparison.OrdinalIgnoreCase))
            {
                var ancestorOrgIds = GetAncestorOrgIds(selectedOrgId.Value, userContext);

                var inheritedResult = await _domainFacade.SearchInheritedInterviewGuides(
                    groupId.Value,
                    ancestorOrgIds,
                    request.Name,
                    request.IsActive,
                    request.SortBy,
                    request.PageNumber,
                    request.PageSize);

                return Ok(new PaginatedResponse<InterviewGuideResource>
                {
                    Items = InterviewGuideMapper.ToResource(inheritedResult.Items, isInherited: true, orgNameLookup: orgNameLookup),
                    TotalCount = inheritedResult.TotalCount,
                    PageNumber = inheritedResult.PageNumber,
                    PageSize = inheritedResult.PageSize
                });
            }
        }

        // Legacy behavior: return all guides the user can access
        var orgFilter = (userContext is { IsResolved: true, IsSuperadmin: false, IsGroupAdmin: false })
            ? userContext.AccessibleOrganizationIds
            : null;

        var result = await _domainFacade.SearchInterviewGuides(
            groupId,
            request.Name,
            request.IsActive,
            request.SortBy,
            request.PageNumber,
            request.PageSize,
            orgFilter);

        var response = new PaginatedResponse<InterviewGuideResource>
        {
            Items = InterviewGuideMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        _logger.LogInformation("Search returned {Count} InterviewGuides", result.TotalCount);
        return Ok(response);
    }

    /// <summary>
    /// Updates an interview guide
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(InterviewGuideResource), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<InterviewGuideResource>> Update(Guid id, [FromBody] UpdateInterviewGuideResource resource)
    {
        _logger.LogInformation("Updating InterviewGuide with ID: {Id}", id);

        var existingGuide = await _domainFacade.GetInterviewGuideById(id);
        if (existingGuide == null)
        {
            return NotFound(new ErrorResponse { Message = $"Interview guide with ID {id} not found" });
        }

        // Ownership guard: only the creating org can edit a guide
        var selectedOrgId = GetSelectedOrganizationId();
        if (selectedOrgId.HasValue && existingGuide.OrganizationId.HasValue
            && selectedOrgId.Value != existingGuide.OrganizationId.Value)
        {
            return StatusCode(403, "Cannot edit an interview guide owned by a different organization.");
        }

        var guideToUpdate = InterviewGuideMapper.ToDomain(resource, existingGuide);

        InterviewGuide updatedGuide;

        if (resource.Questions != null)
        {
            var questions = InterviewGuideMapper.ToQuestionsDomain(resource.Questions);
            updatedGuide = await _domainFacade.UpdateInterviewGuideWithQuestions(guideToUpdate, questions);
        }
        else
        {
            updatedGuide = await _domainFacade.UpdateInterviewGuide(guideToUpdate);
            updatedGuide = (await _domainFacade.GetInterviewGuideByIdWithQuestions(id))!;
        }

        var response = InterviewGuideMapper.ToResource(updatedGuide);
        _logger.LogInformation("Successfully updated InterviewGuide with ID: {Id}", id);

        return Ok(response);
    }

    /// <summary>
    /// Deletes an interview guide
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Deleting InterviewGuide with ID: {Id}", id);

        // Ownership guard: only the creating org can delete a guide
        var selectedOrgId = GetSelectedOrganizationId();
        var existingGuide = await _domainFacade.GetInterviewGuideById(id);
        if (existingGuide != null && selectedOrgId.HasValue && existingGuide.OrganizationId.HasValue
            && selectedOrgId.Value != existingGuide.OrganizationId.Value)
        {
            return StatusCode(403, "Cannot delete an interview guide owned by a different organization.");
        }

        var deleted = await _domainFacade.DeleteInterviewGuide(id);
        if (!deleted)
        {
            return NotFound(new ErrorResponse { Message = $"Interview guide with ID {id} not found" });
        }

        _logger.LogInformation("Successfully deleted InterviewGuide with ID: {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Clones an inherited interview guide into the currently selected organization as a local guide.
    /// </summary>
    [HttpPost("{id}/clone")]
    [ProducesResponseType(typeof(InterviewGuideResource), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<InterviewGuideResource>> Clone(Guid id)
    {
        var userContext = GetUserContext();
        var selectedOrgId = GetSelectedOrganizationId();
        var groupId = userContext?.GroupId;

        if (!selectedOrgId.HasValue)
        {
            return BadRequest("An organization must be selected to clone an interview guide.");
        }

        if (!groupId.HasValue)
        {
            return BadRequest("Group context is required.");
        }

        var clonedGuide = await _domainFacade.CloneInterviewGuide(id, selectedOrgId.Value, groupId.Value);
        var response = InterviewGuideMapper.ToResource(clonedGuide);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets all questions for a guide
    /// </summary>
    [HttpGet("{id}/questions")]
    [ProducesResponseType(typeof(List<InterviewGuideQuestionResource>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<List<InterviewGuideQuestionResource>>> GetQuestions(Guid id)
    {
        var guide = await _domainFacade.GetInterviewGuideById(id);
        if (guide == null)
        {
            return NotFound(new ErrorResponse { Message = $"Interview guide with ID {id} not found" });
        }

        var questions = await _domainFacade.GetInterviewGuideQuestions(id);
        var response = questions.Select(InterviewGuideMapper.ToQuestionResource).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Adds a question to a guide
    /// </summary>
    [HttpPost("{id}/questions")]
    [ProducesResponseType(typeof(InterviewGuideQuestionResource), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<InterviewGuideQuestionResource>> AddQuestion(Guid id, [FromBody] CreateInterviewGuideQuestionResource resource)
    {
        var guide = await _domainFacade.GetInterviewGuideById(id);
        if (guide == null)
        {
            return NotFound(new ErrorResponse { Message = $"Interview guide with ID {id} not found" });
        }

        var question = new InterviewGuideQuestion
        {
            InterviewGuideId = id,
            Question = resource.Question,
            DisplayOrder = resource.DisplayOrder,
            ScoringWeight = resource.ScoringWeight,
            ScoringGuidance = resource.ScoringGuidance,
            FollowUpsEnabled = resource.FollowUpsEnabled,
            MaxFollowUps = resource.MaxFollowUps
        };

        var createdQuestion = await _domainFacade.AddInterviewGuideQuestion(question);
        var response = InterviewGuideMapper.ToQuestionResource(createdQuestion);

        return CreatedAtAction(nameof(GetQuestions), new { id }, response);
    }

    /// <summary>
    /// Deletes a question from a guide
    /// </summary>
    [HttpDelete("{id}/questions/{questionId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> DeleteQuestion(Guid id, Guid questionId)
    {
        var deleted = await _domainFacade.DeleteInterviewGuideQuestion(questionId);
        if (!deleted)
        {
            return NotFound(new ErrorResponse { Message = $"Question with ID {questionId} not found" });
        }

        return NoContent();
    }
}
