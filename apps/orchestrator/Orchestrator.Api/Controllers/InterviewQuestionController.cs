using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for Interview Question follow-up management
/// </summary>
[ApiController]
[Route("api/v1/interview-questions")]
[Produces("application/json")]
[Authorize]
public class InterviewQuestionController : ControllerBase
{
    private readonly DomainFacade _domainFacade;
    private readonly ILogger<InterviewQuestionController> _logger;

    public InterviewQuestionController(DomainFacade domainFacade, ILogger<InterviewQuestionController> logger)
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
        _logger.LogInformation("Generating follow-up suggestions for question {QuestionId}", id);

        try
        {
            var suggestions = await _domainFacade.GenerateFollowUpSuggestions(id);
            var response = FollowUpMapper.ToResources(suggestions);
            return Ok(response);
        }
        catch (InterviewQuestionNotFoundException ex)
        {
            _logger.LogWarning("Question not found: {Message}", ex.Message);
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
        catch (FollowUpGenerationException ex)
        {
            _logger.LogError(ex, "Failed to generate follow-up suggestions");
            return BadRequest(new ErrorResponse { Message = $"Failed to generate follow-up suggestions: {ex.Message}" });
        }
    }

    /// <summary>
    /// Approves selected follow-up templates
    /// </summary>
    [HttpPost("{id}/follow-ups/approve")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult> ApproveFollowUps(Guid id, [FromBody] ApproveFollowUpsResource resource)
    {
        _logger.LogInformation("Approving {Count} follow-ups for question {QuestionId}", resource.TemplateIds.Count, id);

        try
        {
            // Verify question exists
            var question = await _domainFacade.GetInterviewQuestionById(id);
            if (question == null)
            {
                return NotFound(new ErrorResponse { Message = $"Interview question with ID {id} not found" });
            }

            await _domainFacade.ApproveFollowUps(resource.TemplateIds);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve follow-ups");
            return BadRequest(new ErrorResponse { Message = $"Failed to approve follow-ups: {ex.Message}" });
        }
    }

    /// <summary>
    /// Gets all follow-up templates for a question
    /// </summary>
    [HttpGet("{id}/follow-ups")]
    [ProducesResponseType(typeof(IEnumerable<FollowUpTemplateResource>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<IEnumerable<FollowUpTemplateResource>>> GetFollowUps(Guid id)
    {
        _logger.LogInformation("Getting follow-ups for question {QuestionId}", id);

        var question = await _domainFacade.GetInterviewQuestionById(id);
        if (question == null)
        {
            return NotFound(new ErrorResponse { Message = $"Interview question with ID {id} not found" });
        }

        var templates = await _domainFacade.GetFollowUpTemplatesByQuestionId(id);
        var response = FollowUpMapper.ToResources(templates);
        return Ok(response);
    }

    /// <summary>
    /// Updates a follow-up template
    /// </summary>
    [HttpPut("{id}/follow-ups/{templateId}")]
    [ProducesResponseType(typeof(FollowUpTemplateResource), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<FollowUpTemplateResource>> UpdateFollowUp(Guid id, Guid templateId, [FromBody] UpdateFollowUpTemplateResource resource)
    {
        _logger.LogInformation("Updating follow-up {TemplateId} for question {QuestionId}", templateId, id);

        var question = await _domainFacade.GetInterviewQuestionById(id);
        if (question == null)
        {
            return NotFound(new ErrorResponse { Message = $"Interview question with ID {id} not found" });
        }

        var existing = await _domainFacade.GetFollowUpTemplateById(templateId);
        if (existing == null || existing.InterviewQuestionId != id)
        {
            return NotFound(new ErrorResponse { Message = $"Follow-up template with ID {templateId} not found" });
        }

        var updated = FollowUpMapper.ToDomain(resource, existing);
        var saved = await _domainFacade.UpdateFollowUpTemplate(updated);
        var response = FollowUpMapper.ToResource(saved);
        return Ok(response);
    }

    /// <summary>
    /// Deletes a follow-up template
    /// </summary>
    [HttpDelete("{id}/follow-ups/{templateId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult> DeleteFollowUp(Guid id, Guid templateId)
    {
        _logger.LogInformation("Deleting follow-up {TemplateId} for question {QuestionId}", templateId, id);

        var question = await _domainFacade.GetInterviewQuestionById(id);
        if (question == null)
        {
            return NotFound(new ErrorResponse { Message = $"Interview question with ID {id} not found" });
        }

        var existing = await _domainFacade.GetFollowUpTemplateById(templateId);
        if (existing == null || existing.InterviewQuestionId != id)
        {
            return NotFound(new ErrorResponse { Message = $"Follow-up template with ID {templateId} not found" });
        }

        var deleted = await _domainFacade.DeleteFollowUpTemplate(templateId);
        if (!deleted)
        {
            return NotFound(new ErrorResponse { Message = $"Follow-up template with ID {templateId} not found" });
        }

        return NoContent();
    }
}
