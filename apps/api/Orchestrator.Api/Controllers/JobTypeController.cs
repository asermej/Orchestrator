using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for JobType management operations
/// </summary>
[ApiController]
[Route("api/v1/job-types")]
[Produces("application/json")]
[Authorize]
public class JobTypeController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public JobTypeController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Creates a new job type
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(JobTypeResource), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<JobTypeResource>> Create([FromBody] CreateJobTypeResource resource)
    {
        // Get or create organization ID
        var organizationId = resource.OrganizationId;
        if (!organizationId.HasValue || organizationId == Guid.Empty)
        {
            // Get or create default organization for this deployment
            var defaultOrg = await _domainFacade.GetOrCreateDefaultOrganization();
            organizationId = defaultOrg.Id;
        }
        
        var jobType = JobTypeMapper.ToDomain(resource, organizationId.Value);
        var created = await _domainFacade.CreateJobType(jobType);

        // Add questions if provided
        if (resource.Questions?.Any() == true)
        {
            var order = 0;
            foreach (var questionResource in resource.Questions)
            {
                if (questionResource.QuestionOrder == 0)
                {
                    questionResource.QuestionOrder = order++;
                }
                var question = JobTypeMapper.ToQuestionDomain(questionResource, created.Id);
                await _domainFacade.AddInterviewQuestion(question);
            }
        }

        var questions = await _domainFacade.GetInterviewQuestionsByJobTypeId(created.Id);
        var response = JobTypeMapper.ToResource(created, questions);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets a job type by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(JobTypeResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<JobTypeResource>> GetById(Guid id)
    {
        var jobType = await _domainFacade.GetJobTypeById(id);
        if (jobType == null)
        {
            return NotFound($"JobType with ID {id} not found");
        }

        var questions = await _domainFacade.GetInterviewQuestionsByJobTypeId(id);
        return Ok(JobTypeMapper.ToResource(jobType, questions));
    }

    /// <summary>
    /// Searches for job types
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<JobTypeResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<JobTypeResource>>> Search([FromQuery] SearchJobTypeRequest request)
    {
        var result = await _domainFacade.SearchJobTypes(
            request.OrganizationId,
            request.Name,
            request.IsActive,
            request.PageNumber,
            request.PageSize);

        // Fetch question counts for each job type
        var resources = new List<JobTypeResource>();
        foreach (var jobType in result.Items)
        {
            var questions = await _domainFacade.GetInterviewQuestionsByJobTypeId(jobType.Id);
            resources.Add(JobTypeMapper.ToResource(jobType, questions));
        }

        var response = new PaginatedResponse<JobTypeResource>
        {
            Items = resources,
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Updates a job type
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(JobTypeResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<JobTypeResource>> Update(Guid id, [FromBody] UpdateJobTypeResource resource)
    {
        var existing = await _domainFacade.GetJobTypeById(id);
        if (existing == null)
        {
            return NotFound($"JobType with ID {id} not found");
        }

        var jobType = JobTypeMapper.ToDomain(resource, existing);
        var updated = await _domainFacade.UpdateJobType(jobType);

        var questions = await _domainFacade.GetInterviewQuestionsByJobTypeId(id);
        return Ok(JobTypeMapper.ToResource(updated, questions));
    }

    /// <summary>
    /// Deletes a job type
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _domainFacade.DeleteJobType(id);
        if (!deleted)
        {
            return NotFound($"JobType with ID {id} not found");
        }
        return NoContent();
    }

    // Question endpoints

    /// <summary>
    /// Adds a question to a job type
    /// </summary>
    [HttpPost("{id}/questions")]
    [ProducesResponseType(typeof(InterviewQuestionResource), 201)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewQuestionResource>> AddQuestion(Guid id, [FromBody] CreateInterviewQuestionResource resource)
    {
        var jobType = await _domainFacade.GetJobTypeById(id);
        if (jobType == null)
        {
            return NotFound($"JobType with ID {id} not found");
        }

        var question = JobTypeMapper.ToQuestionDomain(resource, id);
        var created = await _domainFacade.AddInterviewQuestion(question);
        return Created($"/api/v1/job-types/{id}/questions/{created.Id}", JobTypeMapper.ToQuestionResource(created));
    }

    /// <summary>
    /// Gets all questions for a job type
    /// </summary>
    [HttpGet("{id}/questions")]
    [ProducesResponseType(typeof(IEnumerable<InterviewQuestionResource>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<InterviewQuestionResource>>> GetQuestions(Guid id)
    {
        var jobType = await _domainFacade.GetJobTypeById(id);
        if (jobType == null)
        {
            return NotFound($"JobType with ID {id} not found");
        }

        var questions = await _domainFacade.GetInterviewQuestionsByJobTypeId(id);
        return Ok(questions.Select(JobTypeMapper.ToQuestionResource));
    }

    /// <summary>
    /// Updates a question
    /// </summary>
    [HttpPut("{id}/questions/{questionId}")]
    [ProducesResponseType(typeof(InterviewQuestionResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewQuestionResource>> UpdateQuestion(Guid id, Guid questionId, [FromBody] UpdateInterviewQuestionResource resource)
    {
        var existing = await _domainFacade.GetInterviewQuestionById(questionId);
        if (existing == null || existing.JobTypeId != id)
        {
            return NotFound($"Question with ID {questionId} not found");
        }

        var question = JobTypeMapper.ToQuestionDomain(resource, existing);
        var updated = await _domainFacade.UpdateInterviewQuestion(question);
        return Ok(JobTypeMapper.ToQuestionResource(updated));
    }

    /// <summary>
    /// Deletes a question
    /// </summary>
    [HttpDelete("{id}/questions/{questionId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> DeleteQuestion(Guid id, Guid questionId)
    {
        var existing = await _domainFacade.GetInterviewQuestionById(questionId);
        if (existing == null || existing.JobTypeId != id)
        {
            return NotFound($"Question with ID {questionId} not found");
        }

        await _domainFacade.DeleteInterviewQuestion(questionId);
        return NoContent();
    }
}
