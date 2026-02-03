using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for Interview management and execution
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class InterviewController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public InterviewController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Creates a new interview
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(InterviewResource), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<InterviewResource>> Create([FromBody] CreateInterviewResource resource)
    {
        var interview = InterviewMapper.ToDomain(resource);
        var created = await _domainFacade.CreateInterview(interview);
        var response = InterviewMapper.ToResource(created);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets an interview by ID (requires authentication)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(InterviewResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewResource>> GetById(Guid id)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }
        return Ok(InterviewMapper.ToResource(interview));
    }

    /// <summary>
    /// Gets an interview by token (public access for applicants)
    /// </summary>
    [HttpGet("by-token/{token}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InterviewDetailResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewDetailResource>> GetByToken(string token)
    {
        var interview = await _domainFacade.GetInterviewByToken(token);
        if (interview == null)
        {
            return NotFound($"Interview not found");
        }

        // Get related data
        var job = await _domainFacade.GetJobById(interview.JobId);
        var applicant = await _domainFacade.GetApplicantById(interview.ApplicantId);
        var agent = await _domainFacade.GetAgentById(interview.AgentId);
        var responses = await _domainFacade.GetInterviewResponsesByInterviewId(interview.Id);
        var result = await _domainFacade.GetInterviewResultByInterviewId(interview.Id);

        // Get questions from job type
        IEnumerable<InterviewQuestion> questions = new List<InterviewQuestion>();
        if (job?.JobTypeId != null)
        {
            questions = await _domainFacade.GetInterviewQuestionsByJobTypeId(job.JobTypeId.Value);
        }

        var response = new InterviewDetailResource
        {
            Id = interview.Id,
            JobId = interview.JobId,
            ApplicantId = interview.ApplicantId,
            AgentId = interview.AgentId,
            Token = interview.Token,
            Status = interview.Status,
            InterviewType = interview.InterviewType,
            ScheduledAt = interview.ScheduledAt,
            StartedAt = interview.StartedAt,
            CompletedAt = interview.CompletedAt,
            CurrentQuestionIndex = interview.CurrentQuestionIndex,
            CreatedAt = interview.CreatedAt,
            UpdatedAt = interview.UpdatedAt,
            Job = job != null ? JobMapper.ToResource(job) : null,
            Applicant = applicant != null ? ApplicantMapper.ToResource(applicant) : null,
            Agent = agent != null ? AgentMapper.ToResource(agent) : null,
            Questions = questions.Select(JobTypeMapper.ToQuestionResource).ToList(),
            Responses = responses.Select(InterviewMapper.ToResponseResource).ToList(),
            Result = result != null ? InterviewMapper.ToResultResource(result) : null
        };

        return Ok(response);
    }

    /// <summary>
    /// Searches for interviews
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PaginatedResponse<InterviewResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<InterviewResource>>> Search([FromQuery] SearchInterviewRequest request)
    {
        var result = await _domainFacade.SearchInterviews(
            request.JobId,
            request.ApplicantId,
            request.AgentId,
            request.Status,
            request.PageNumber,
            request.PageSize);

        var response = new PaginatedResponse<InterviewResource>
        {
            Items = InterviewMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Starts an interview (public access by token)
    /// </summary>
    [HttpPost("by-token/{token}/start")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InterviewResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewResource>> StartByToken(string token)
    {
        var interview = await _domainFacade.GetInterviewByToken(token);
        if (interview == null)
        {
            return NotFound($"Interview not found");
        }

        var started = await _domainFacade.StartInterview(interview.Id);
        return Ok(InterviewMapper.ToResource(started));
    }

    /// <summary>
    /// Completes an interview (public access by token)
    /// </summary>
    [HttpPost("by-token/{token}/complete")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InterviewResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewResource>> CompleteByToken(string token)
    {
        var interview = await _domainFacade.GetInterviewByToken(token);
        if (interview == null)
        {
            return NotFound($"Interview not found");
        }

        var completed = await _domainFacade.CompleteInterview(interview.Id);
        return Ok(InterviewMapper.ToResource(completed));
    }

    /// <summary>
    /// Adds a response to an interview (public access by token)
    /// </summary>
    [HttpPost("by-token/{token}/responses")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InterviewResponseResource), 201)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewResponseResource>> AddResponse(string token, [FromBody] CreateInterviewResponseResource resource)
    {
        var interview = await _domainFacade.GetInterviewByToken(token);
        if (interview == null)
        {
            return NotFound($"Interview not found");
        }

        var response = InterviewMapper.ToResponseDomain(resource, interview.Id);
        var created = await _domainFacade.AddInterviewResponse(response);
        return Created($"/api/v1/interview/{interview.Id}/responses/{created.Id}", InterviewMapper.ToResponseResource(created));
    }

    /// <summary>
    /// Gets all responses for an interview
    /// </summary>
    [HttpGet("{id}/responses")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<InterviewResponseResource>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<InterviewResponseResource>>> GetResponses(Guid id)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }

        var responses = await _domainFacade.GetInterviewResponsesByInterviewId(id);
        return Ok(responses.Select(InterviewMapper.ToResponseResource));
    }

    /// <summary>
    /// Adds a response to an interview by ID (for test interviews)
    /// </summary>
    [HttpPost("{id}/responses")]
    [Authorize]
    [ProducesResponseType(typeof(InterviewResponseResource), 201)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewResponseResource>> AddResponseById(Guid id, [FromBody] CreateInterviewResponseResource resource)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }

        var response = InterviewMapper.ToResponseDomain(resource, id);
        var created = await _domainFacade.AddInterviewResponse(response);
        return Created($"/api/v1/interview/{id}/responses/{created.Id}", InterviewMapper.ToResponseResource(created));
    }

    /// <summary>
    /// Creates/updates the result for an interview
    /// </summary>
    [HttpPost("{id}/result")]
    [Authorize]
    [ProducesResponseType(typeof(InterviewResultResource), 201)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewResultResource>> CreateResult(Guid id, [FromBody] CreateInterviewResultResource resource)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }

        // Check if result already exists
        var existing = await _domainFacade.GetInterviewResultByInterviewId(id);
        if (existing != null)
        {
            // Update existing
            existing.Summary = resource.Summary ?? existing.Summary;
            existing.Score = resource.Score ?? existing.Score;
            existing.Recommendation = resource.Recommendation ?? existing.Recommendation;
            existing.Strengths = resource.Strengths ?? existing.Strengths;
            existing.AreasForImprovement = resource.AreasForImprovement ?? existing.AreasForImprovement;
            var updated = await _domainFacade.UpdateInterviewResult(existing);
            return Ok(InterviewMapper.ToResultResource(updated));
        }

        var result = InterviewMapper.ToResultDomain(resource, id);
        var created = await _domainFacade.CreateInterviewResult(result);
        return Created($"/api/v1/interview/{id}/result", InterviewMapper.ToResultResource(created));
    }

    /// <summary>
    /// Gets the result for an interview
    /// </summary>
    [HttpGet("{id}/result")]
    [Authorize]
    [ProducesResponseType(typeof(InterviewResultResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewResultResource>> GetResult(Guid id)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }

        var result = await _domainFacade.GetInterviewResultByInterviewId(id);
        if (result == null)
        {
            return NotFound($"Result for interview {id} not found");
        }

        return Ok(InterviewMapper.ToResultResource(result));
    }

    /// <summary>
    /// Deletes an interview
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _domainFacade.DeleteInterview(id);
        if (!deleted)
        {
            return NotFound($"Interview with ID {id} not found");
        }
        return NoContent();
    }

    // Test Interview Endpoints

    /// <summary>
    /// Creates a test interview from an interview configuration
    /// </summary>
    [HttpPost("test")]
    [Authorize]
    [ProducesResponseType(typeof(InterviewResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewResource>> CreateTestInterview([FromBody] CreateTestInterviewRequest request)
    {
        var interview = await _domainFacade.CreateTestInterview(request.InterviewConfigurationId, request.TestUserName);
        return CreatedAtAction(nameof(GetById), new { id = interview.Id }, InterviewMapper.ToResource(interview));
    }

    /// <summary>
    /// Scores a completed test interview
    /// </summary>
    [HttpPost("test/{id}/score")]
    [Authorize]
    [ProducesResponseType(typeof(InterviewResultResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewResultResource>> ScoreTestInterview(Guid id, [FromBody] ScoreTestInterviewRequest request)
    {
        var result = await _domainFacade.ScoreTestInterview(id, request.InterviewConfigurationId);
        return Ok(InterviewMapper.ToResultResource(result));
    }

    /// <summary>
    /// Warms up audio cache for test interview questions by interview ID.
    /// Pre-generates TTS audio for all questions to reduce latency during interview.
    /// </summary>
    /// <param name="id">The interview ID</param>
    /// <returns>Warmup result with cache statistics</returns>
    /// <response code="200">Returns the warmup result</response>
    /// <response code="404">If the interview is not found</response>
    [HttpPost("{id}/audio/warmup")]
    [Authorize]
    [ProducesResponseType(typeof(InterviewAudioWarmupResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewAudioWarmupResource>> WarmupAudioById(Guid id)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound("Interview not found");
        }

        var result = await _domainFacade.WarmupInterviewAudioAsync(interview.Id, HttpContext.RequestAborted);
        
        return Ok(new InterviewAudioWarmupResource
        {
            InterviewId = result.InterviewId,
            TotalQuestions = result.TotalQuestions,
            CachedQuestions = result.CachedQuestions,
            AlreadyCached = result.AlreadyCached,
            FailedQuestions = result.FailedQuestions,
            IsComplete = result.IsComplete
        });
    }

    /// <summary>
    /// Warms up audio cache for interview questions (public access by token).
    /// Pre-generates TTS audio for all questions to reduce latency during interview.
    /// </summary>
    /// <param name="token">The interview token</param>
    /// <returns>Warmup result with cache statistics</returns>
    /// <response code="200">Returns the warmup result</response>
    /// <response code="404">If the interview is not found</response>
    [HttpPost("by-token/{token}/audio/warmup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InterviewAudioWarmupResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewAudioWarmupResource>> WarmupAudioByToken(string token)
    {
        var interview = await _domainFacade.GetInterviewByToken(token);
        if (interview == null)
        {
            return NotFound("Interview not found");
        }

        var result = await _domainFacade.WarmupInterviewAudioAsync(interview.Id, HttpContext.RequestAborted);
        
        return Ok(new InterviewAudioWarmupResource
        {
            InterviewId = result.InterviewId,
            TotalQuestions = result.TotalQuestions,
            CachedQuestions = result.CachedQuestions,
            AlreadyCached = result.AlreadyCached,
            FailedQuestions = result.FailedQuestions,
            IsComplete = result.IsComplete
        });
    }
}

/// <summary>
/// Response for audio warmup endpoint
/// </summary>
public class InterviewAudioWarmupResource
{
    public Guid InterviewId { get; set; }
    public int TotalQuestions { get; set; }
    public int CachedQuestions { get; set; }
    public int AlreadyCached { get; set; }
    public int FailedQuestions { get; set; }
    public bool IsComplete { get; set; }
}
