using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;
using Orchestrator.Api.Middleware;
using System.Diagnostics;

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

    private UserContext? GetUserContext()
        => HttpContext.Items.TryGetValue("UserContext", out var ctx) ? ctx as UserContext : null;

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
    /// Gets an interview by ID with full detail (requires authentication)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(InterviewDetailResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewDetailResource>> GetById(Guid id)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }

        // Get related data
        var job = await _domainFacade.GetJobById(interview.JobId);
        var applicant = await _domainFacade.GetApplicantById(interview.ApplicantId);
        var agent = await _domainFacade.GetAgentById(interview.AgentId);
        var responses = await _domainFacade.GetInterviewResponsesByInterviewId(interview.Id);
        var result = await _domainFacade.GetInterviewResultByInterviewId(interview.Id);

        var response = new InterviewDetailResource
        {
            Id = interview.Id,
            JobId = interview.JobId,
            ApplicantId = interview.ApplicantId,
            AgentId = interview.AgentId,
            InterviewConfigurationId = interview.InterviewConfigurationId,
            InterviewGuideId = interview.InterviewGuideId,
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
            Questions = new List<InterviewQuestionResource>(),
            Responses = responses.Select(InterviewMapper.ToResponseResource).ToList(),
            Result = result != null ? InterviewMapper.ToResultResource(result) : null
        };

        return Ok(response);
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

        var response = new InterviewDetailResource
        {
            Id = interview.Id,
            JobId = interview.JobId,
            ApplicantId = interview.ApplicantId,
            AgentId = interview.AgentId,
            InterviewConfigurationId = interview.InterviewConfigurationId,
            InterviewGuideId = interview.InterviewGuideId,
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
            Questions = new List<InterviewQuestionResource>(),
            Responses = responses.Select(InterviewMapper.ToResponseResource).ToList(),
            Result = result != null ? InterviewMapper.ToResultResource(result) : null
        };

        return Ok(response);
    }

    /// <summary>
    /// Searches for interviews with related entity data
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PaginatedResponse<InterviewResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<InterviewResource>>> Search([FromQuery] SearchInterviewRequest request)
    {
        var userContext = GetUserContext();
        var groupId = userContext?.GroupId;

        var result = await _domainFacade.SearchInterviews(
            groupId,
            request.JobId,
            request.ApplicantId,
            request.AgentId,
            request.Status,
            request.PageNumber,
            request.PageSize);

        // Collect unique related entity IDs for batch lookup
        var jobIds = result.Items.Select(i => i.JobId).Distinct().ToList();
        var applicantIds = result.Items.Select(i => i.ApplicantId).Distinct().ToList();
        var agentIds = result.Items.Select(i => i.AgentId).Distinct().ToList();
        var interviewIds = result.Items.Select(i => i.Id).ToList();

        // Fetch related entities in parallel
        var jobTasks = jobIds.Select(id => _domainFacade.GetJobById(id));
        var applicantTasks = applicantIds.Select(id => _domainFacade.GetApplicantById(id));
        var agentTasks = agentIds.Select(id => _domainFacade.GetAgentById(id));
        var responseTasks = interviewIds.Select(id => _domainFacade.GetInterviewResponsesByInterviewId(id));
        var resultTasks = interviewIds.Select(id => _domainFacade.GetInterviewResultByInterviewId(id));

        var jobs = (await Task.WhenAll(jobTasks)).Where(j => j != null).ToDictionary(j => j!.Id);
        var applicants = (await Task.WhenAll(applicantTasks)).Where(a => a != null).ToDictionary(a => a!.Id);
        var agents = (await Task.WhenAll(agentTasks)).Where(a => a != null).ToDictionary(a => a!.Id);
        var responsesByInterview = (await Task.WhenAll(responseTasks))
            .Select((responses, index) => new { InterviewId = interviewIds[index], Responses = responses })
            .ToDictionary(x => x.InterviewId, x => x.Responses);
        var resultsByInterview = (await Task.WhenAll(resultTasks))
            .Select((r, index) => new { InterviewId = interviewIds[index], Result = r })
            .Where(x => x.Result != null)
            .ToDictionary(x => x.InterviewId, x => x.Result!);

        // Map interviews with hydrated related data
        var items = result.Items.Select(interview =>
        {
            var resource = InterviewMapper.ToResource(interview);
            resource.Job = jobs.TryGetValue(interview.JobId, out var job) && job != null ? JobMapper.ToResource(job) : null;
            resource.Applicant = applicants.TryGetValue(interview.ApplicantId, out var applicant) && applicant != null ? ApplicantMapper.ToResource(applicant) : null;
            resource.Agent = agents.TryGetValue(interview.AgentId, out var agent) && agent != null ? AgentMapper.ToResource(agent) : null;
            resource.Responses = responsesByInterview.TryGetValue(interview.Id, out var responses)
                ? responses.Select(InterviewMapper.ToResponseResource).ToList()
                : new List<InterviewResponseResource>();
            resource.Result = resultsByInterview.TryGetValue(interview.Id, out var interviewResult)
                ? InterviewMapper.ToResultResource(interviewResult)
                : null;
            return resource;
        });

        var response = new PaginatedResponse<InterviewResource>
        {
            Items = items,
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
    [ProducesResponseType(typeof(FollowUpSelectionResponseResource), 201)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<FollowUpSelectionResponseResource>> AddResponse(string token, [FromBody] CreateInterviewResponseResource resource)
    {
        var interview = await _domainFacade.GetInterviewByToken(token);
        if (interview == null)
        {
            return NotFound($"Interview not found");
        }

        var response = InterviewMapper.ToResponseDomain(resource, interview.Id);
        
        // Set question type based on is_follow_up
        if (resource.IsFollowUp)
        {
            response.QuestionType = "followup";
            // If FollowUpTemplateId is provided, use it
            if (resource.FollowUpTemplateId.HasValue)
            {
                response.FollowUpTemplateId = resource.FollowUpTemplateId;
            }
        }
        else
        {
            response.QuestionType = "main";
        }

        var created = await _domainFacade.AddInterviewResponse(response);

        // Check if we should ask a follow-up question
        string nextQuestionType = "main";
        FollowUpSelectionResult? followUpResult = null;
        FollowUpTemplate? selectedTemplate = null;

        // Only check for follow-up if this was a main question (not already a follow-up)
        if (!resource.IsFollowUp && resource.QuestionId.HasValue)
        {
            try
            {
                followUpResult = await _domainFacade.SelectAndReturnFollowUp(
                    interview.Id,
                    resource.QuestionId.Value,
                    resource.Transcript ?? string.Empty);

                if (followUpResult.SelectedTemplateId.HasValue)
                {
                    selectedTemplate = await _domainFacade.GetFollowUpTemplateById(followUpResult.SelectedTemplateId.Value);
                    nextQuestionType = "followup";
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the response save
                Debug.WriteLine($"Error selecting follow-up: {ex.Message}");
            }
        }

        // Check if interview is complete (all main questions answered)
        var allResponses = await _domainFacade.GetInterviewResponsesByInterviewId(interview.Id);
        var mainQuestionResponses = allResponses.Where(r => r.QuestionType == "main").ToList();
        
        // Get questions from job
        var job = await _domainFacade.GetJobById(interview.JobId);
        int totalMainQuestions = 0;

        if (mainQuestionResponses.Count >= totalMainQuestions && nextQuestionType != "followup")
        {
            nextQuestionType = "complete";
        }

        var followUpResponse = FollowUpMapper.ToResource(
            followUpResult ?? new FollowUpSelectionResult { SelectedTemplateId = null },
            selectedTemplate,
            nextQuestionType);

        return Created($"/api/v1/interview/{interview.Id}/responses/{created.Id}", followUpResponse);
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
