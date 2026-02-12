using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for Interview Configuration Question follow-up management
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class InterviewConfigurationQuestionController : ControllerBase
{
    private readonly DomainFacade _domainFacade;
    private readonly ILogger<InterviewConfigurationQuestionController> _logger;

    public InterviewConfigurationQuestionController(DomainFacade domainFacade, ILogger<InterviewConfigurationQuestionController> logger)
    {
        _domainFacade = domainFacade;
        _logger = logger;
    }

    /// <summary>
    /// Generates AI suggestions for follow-up questions
    /// </summary>
    [HttpPost("{id}/follow-ups/generate")]
    [ProducesResponseType(typeof(IEnumerable<FollowUpSuggestionResource>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<IEnumerable<FollowUpSuggestionResource>>> GenerateFollowUps(Guid id)
    {
        _logger.LogInformation("Generating follow-up suggestions for configuration question {QuestionId}", id);

        try
        {
            var suggestions = await _domainFacade.GenerateFollowUpSuggestionsForConfigQuestion(id);
            var response = FollowUpMapper.ToResources(suggestions);
            return Ok(response);
        }
        catch (InterviewConfigurationQuestionNotFoundException ex)
        {
            _logger.LogWarning("Configuration question not found: {Message}", ex.Message);
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
        catch (FollowUpGenerationException ex)
        {
            _logger.LogError(ex, "Failed to generate follow-up suggestions");
            return BadRequest(new ErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Approves selected follow-up templates
    /// </summary>
    [HttpPost("follow-ups/approve")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult> ApproveFollowUps([FromBody] ApproveFollowUpsResource resource)
    {
        _logger.LogInformation("Approving {Count} follow-up templates", resource.TemplateIds?.Count ?? 0);

        try
        {
            if (resource.TemplateIds == null || !resource.TemplateIds.Any())
            {
                return BadRequest(new ErrorResponse { Message = "Template IDs are required" });
            }

            await _domainFacade.ApproveFollowUps(resource.TemplateIds);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve follow-ups");
            return BadRequest(new ErrorResponse { Message = "Failed to approve follow-ups" });
        }
    }

    /// <summary>
    /// Gets all follow-up templates for a configuration question
    /// </summary>
    [HttpGet("{id}/follow-ups")]
    [ProducesResponseType(typeof(IEnumerable<FollowUpTemplateResource>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<IEnumerable<FollowUpTemplateResource>>> GetFollowUps(Guid id)
    {
        _logger.LogInformation("Getting follow-up templates for configuration question {QuestionId}", id);

        try
        {
            var templates = await _domainFacade.GetFollowUpTemplatesByConfigQuestionId(id);
            var response = FollowUpMapper.ToResources(templates);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get follow-up templates");
            return BadRequest(new ErrorResponse { Message = "Failed to get follow-up templates" });
        }
    }

    /// <summary>
    /// Updates a follow-up template
    /// </summary>
    [HttpPut("follow-ups/{templateId}")]
    [ProducesResponseType(typeof(FollowUpTemplateResource), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<FollowUpTemplateResource>> UpdateFollowUp(Guid templateId, [FromBody] UpdateFollowUpTemplateResource resource)
    {
        _logger.LogInformation("Updating follow-up template {TemplateId}", templateId);

        try
        {
            var existing = await _domainFacade.GetFollowUpTemplateById(templateId);
            if (existing == null)
            {
                return NotFound(new ErrorResponse { Message = "Follow-up template not found" });
            }

            var updated = FollowUpMapper.ToDomain(resource, existing);
            var saved = await _domainFacade.UpdateFollowUpTemplate(updated);
            var response = FollowUpMapper.ToResource(saved);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update follow-up template");
            return BadRequest(new ErrorResponse { Message = "Failed to update follow-up template" });
        }
    }

    /// <summary>
    /// Deletes a follow-up template
    /// </summary>
    [HttpDelete("follow-ups/{templateId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult> DeleteFollowUp(Guid templateId)
    {
        _logger.LogInformation("Deleting follow-up template {TemplateId}", templateId);

        try
        {
            var deleted = await _domainFacade.DeleteFollowUpTemplate(templateId);
            if (!deleted)
            {
                return NotFound(new ErrorResponse { Message = "Follow-up template not found" });
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete follow-up template");
            return BadRequest(new ErrorResponse { Message = "Failed to delete follow-up template" });
        }
    }
}
