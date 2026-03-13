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
    /// Gets all competency responses for an interview (per-competency holistic scores)
    /// </summary>
    [HttpGet("{id}/competency-responses")]
    [Authorize]
    [ProducesResponseType(typeof(List<CompetencyResponseResource>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<List<CompetencyResponseResource>>> GetCompetencyResponses(Guid id)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }

        var list = await _domainFacade.GetCompetencyResponsesByInterviewId(id);
        return Ok(list.Select(InterviewMapper.ToResource).ToList());
    }

    /// <summary>
    /// Creates or updates a competency response for an interview (upsert by interview_id + competency_id)
    /// </summary>
    [HttpPut("{id}/competency-responses")]
    [Authorize]
    [ProducesResponseType(typeof(CompetencyResponseResource), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CompetencyResponseResource>> UpsertCompetencyResponse(Guid id, [FromBody] UpsertCompetencyResponseResource resource)
    {
        var interview = await _domainFacade.GetInterviewById(id);
        if (interview == null)
        {
            return NotFound($"Interview with ID {id} not found");
        }

        var domain = InterviewMapper.ToCompetencyResponseDomain(resource, id);
        var saved = await _domainFacade.UpsertCompetencyResponse(domain);
        return Ok(InterviewMapper.ToResource(saved));
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
    /// Creates a test interview from an interview template
    /// </summary>
    [HttpPost("test")]
    [Authorize]
    [ProducesResponseType(typeof(InterviewResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewResource>> CreateTestInterview([FromBody] CreateTestInterviewRequest request)
    {
        if (!request.InterviewTemplateId.HasValue)
            return BadRequest("InterviewTemplateId is required.");

        var interview = await _domainFacade.CreateTestInterviewFromTemplate(request.InterviewTemplateId.Value, request.TestUserName);
        return CreatedAtAction(nameof(GetById), new { id = interview.Id }, InterviewMapper.ToResource(interview));
    }

    // Runtime Endpoints

    /// <summary>
    /// Loads the full runtime context for a template-based interview.
    /// Returns competencies, questions, agent info, and resolved templates.
    /// </summary>
    [HttpGet("{id}/runtime")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InterviewRuntimeContextResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewRuntimeContextResource>> GetRuntimeContext(Guid id)
    {
        var context = await _domainFacade.LoadInterviewRuntimeContextAsync(id);
        if (context == null)
            return NotFound("Interview has no template assigned or template not found.");

        var systemPrompt = context.Agent != null
            ? _domainFacade.BuildInterviewSystemPrompt(context)
            : null;

        var resource = new InterviewRuntimeContextResource
        {
            InterviewId = context.Interview.Id,
            AgentName = context.Agent?.DisplayName ?? "Interviewer",
            ApplicantName = context.ApplicantName,
            JobTitle = context.JobTitle,
            RoleName = context.Role.RoleName,
            Industry = context.Role.Industry,
            OpeningText = _domainFacade.ResolveOpeningTemplate(context),
            ClosingText = _domainFacade.ResolveClosingTemplate(context),
            Competencies = context.Competencies.Select(c => new RuntimeCompetencyResource
            {
                CompetencyId = c.Id,
                Name = c.Name,
                Description = c.Description,
                ScoringWeight = c.DefaultWeight,
                DisplayOrder = c.DisplayOrder,
                PrimaryQuestion = c.CanonicalExample ?? $"Tell me about a time when you demonstrated {c.Name}."
            }).ToList()
        };

        return Ok(resource);
    }

    /// <summary>
    /// Generates the primary AI interview question for a competency.
    /// Call this before asking the candidate, so the exact question text can be
    /// submitted back when completing the competency.
    /// </summary>
    [HttpPost("{id}/runtime/generate-question")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GeneratedQuestionResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<GeneratedQuestionResource>> GenerateQuestion(
        Guid id,
        [FromBody] GenerateQuestionRequest request)
    {
        var context = await _domainFacade.LoadInterviewRuntimeContextAsync(id);
        if (context == null)
            return NotFound("Interview has no template assigned or template not found.");

        var competency = context.Competencies.FirstOrDefault(c => c.Id == request.CompetencyId);
        if (competency == null)
            return NotFound($"Competency {request.CompetencyId} not found in this interview's role.");

        string systemPromptStatic;
        string? systemPromptInterviewPart;
        if (context.Agent != null)
        {
            var parts = _domainFacade.BuildInterviewSystemPromptParts(context);
            systemPromptStatic = parts.StaticPart;
            systemPromptInterviewPart = parts.InterviewPart;
        }
        else
        {
            systemPromptStatic = "";
            systemPromptInterviewPart = null;
        }

        var question = await _domainFacade.GeneratePrimaryQuestionAsync(
            systemPromptStatic,
            systemPromptInterviewPart,
            competency,
            context.Role.RoleName,
            context.Role.Industry,
            context.JobTitle,
            context.ApplicantName,
            includeTransition: request.IncludeTransition,
            previousCompetencyName: request.PreviousCompetencyName,
            cancellationToken: HttpContext.RequestAborted
        );

        if (string.IsNullOrWhiteSpace(question))
            question = competency.CanonicalExample ?? $"Tell me about your experience with {competency.Name}.";

        return Ok(new GeneratedQuestionResource
        {
            CompetencyId = competency.Id,
            Question = question
        });
    }

    /// <summary>
    /// Scores and records a completed competency from pre-collected conversation data.
    /// Always runs AI evaluation on the full accumulated transcript to produce a holistic
    /// 1-5 score. The client-provided score is ignored — the backend is the source of truth.
    /// </summary>
    [HttpPost("{id}/runtime/competency")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CompetencyResponseResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CompetencyResponseResource>> ProcessCompetency(
        Guid id,
        [FromBody] ProcessCompetencyRequest request)
    {
        var context = await _domainFacade.LoadInterviewRuntimeContextAsync(id);
        if (context == null)
            return NotFound("Interview has no template assigned or template not found.");

        var competency = context.Competencies.FirstOrDefault(c => c.Id == request.CompetencyId);
        if (competency == null)
            return NotFound($"Competency {request.CompetencyId} not found in this interview's role.");

        var systemPrompt = context.Agent != null
            ? _domainFacade.BuildInterviewSystemPrompt(context)
            : "";

        var followUpExchanges = request.FollowUpExchanges?.Select(f => new Orchestrator.Domain.FollowUpExchange
        {
            Question = f.Question,
            Response = f.Response
        }).ToList();

        var transcriptParts = new List<string> { request.CandidateResponse };
        if (followUpExchanges != null)
        {
            foreach (var exchange in followUpExchanges)
                transcriptParts.Add(exchange.Response);
        }
        var fullTranscript = string.Join("\n\n", transcriptParts);

        var evaluation = await _domainFacade.EvaluateCompetencyResponseAsync(
            systemPrompt,
            competency,
            fullTranscript,
            context.Role.RoleName,
            context.Role.Industry,
            cancellationToken: HttpContext.RequestAborted
        );
        evaluation.FollowUpNeeded = false;

        var result = await _domainFacade.ScoreAndRecordCompetencyAsync(
            id,
            competency,
            request.PrimaryQuestion,
            request.CandidateResponse,
            followUpExchanges,
            evaluation
        );

        return Ok(InterviewMapper.ToResource(result));
    }

    /// <summary>
    /// Evaluates a candidate's response holistically and determines if a follow-up is needed.
    /// Supports cumulative context via PriorExchanges for accurate multi-turn evaluation.
    /// Does not score or persist — use for real-time follow-up generation during the interview.
    /// </summary>
    [HttpPost("{id}/runtime/evaluate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CompetencyEvaluationResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CompetencyEvaluationResource>> EvaluateResponse(
        Guid id,
        [FromBody] EvaluateResponseRequest request)
    {
        var context = await _domainFacade.LoadInterviewRuntimeContextAsync(id);
        if (context == null)
            return NotFound("Interview has no template assigned or template not found.");

        var competency = context.Competencies.FirstOrDefault(c => c.Id == request.CompetencyId);
        if (competency == null)
            return NotFound($"Competency {request.CompetencyId} not found in this interview's role.");

        var systemPrompt = context.Agent != null
            ? _domainFacade.BuildInterviewSystemPrompt(context)
            : "";

        var priorExchanges = request.PriorExchanges?.Select(p => new Orchestrator.Domain.PriorExchange
        {
            Question = p.Question,
            Response = p.Response
        }).ToList();

        var evaluation = await _domainFacade.EvaluateCompetencyResponseWithContextAsync(
            systemPrompt,
            competency,
            request.CandidateResponse,
            context.Role.RoleName,
            context.Role.Industry,
            priorExchanges,
            previousFollowUpTarget: request.PreviousFollowUpTarget,
            cancellationToken: HttpContext.RequestAborted
        );

        var followUpQuestion = evaluation.FollowUpNeeded
            ? evaluation.GetEffectiveFollowUpQuestion()
            : null;

        return Ok(new CompetencyEvaluationResource
        {
            CompetencyScore = evaluation.CompetencyScore,
            Rationale = evaluation.Rationale,
            FollowUpNeeded = evaluation.FollowUpNeeded,
            FollowUpTarget = evaluation.FollowUpTarget,
            FollowUpQuestion = followUpQuestion
        });
    }

    /// <summary>
    /// Classifies a candidate's response before STAR evaluation.
    /// Returns a classification indicating how the response should be handled.
    /// This is a pre-processing step — the classifier never scores.
    /// </summary>
    [HttpPost("{id}/runtime/classify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseClassificationResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ResponseClassificationResource>> ClassifyResponse(
        Guid id,
        [FromBody] ClassifyResponseRequest request)
    {
        var context = await _domainFacade.LoadInterviewRuntimeContextAsync(id);
        if (context == null)
            return NotFound("Interview has no template assigned or template not found.");

        var competency = context.Competencies.FirstOrDefault(c => c.Id == request.CompetencyId);
        if (competency == null)
            return NotFound($"Competency {request.CompetencyId} not found in this interview's role.");

        var systemPrompt = context.Agent != null
            ? _domainFacade.BuildInterviewSystemPrompt(context)
            : "";

        var classification = await _domainFacade.ClassifyResponseAsync(
            systemPrompt,
            request.CandidateResponse,
            request.CurrentQuestion,
            competency.Name,
            HttpContext.RequestAborted
        );

        return Ok(new ResponseClassificationResource
        {
            Classification = classification.Classification,
            RequiresResponse = classification.RequiresResponse,
            ResponseText = classification.ResponseText,
            ConsumesRedirect = classification.ConsumesRedirect,
            AbandonCompetency = classification.AbandonCompetency,
            StoreNote = classification.StoreNote
        });
    }

    /// <summary>
    /// Classifies and evaluates a candidate's response in a single round-trip.
    /// If on_topic, runs STAR evaluation immediately and returns both results.
    /// Eliminates the sequential classify-then-evaluate pattern.
    /// </summary>
    [HttpPost("{id}/runtime/classify-and-evaluate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClassifyAndEvaluateResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ClassifyAndEvaluateResource>> ClassifyAndEvaluateResponse(
        Guid id,
        [FromBody] ClassifyAndEvaluateRequest request)
    {
        var context = await _domainFacade.LoadInterviewRuntimeContextAsync(id);
        if (context == null)
            return NotFound("Interview has no template assigned or template not found.");

        var competency = context.Competencies.FirstOrDefault(c => c.Id == request.CompetencyId);
        if (competency == null)
            return NotFound($"Competency {request.CompetencyId} not found in this interview's role.");

        var systemPrompt = context.Agent != null
            ? _domainFacade.BuildInterviewSystemPrompt(context)
            : "";

        var result = await _domainFacade.ClassifyAndEvaluateResponseAsync(
            systemPrompt,
            competency,
            request.CandidateResponse,
            request.CurrentQuestion,
            request.CompetencyTranscript,
            context.Role.RoleName,
            context.Role.Industry,
            request.PreviousFollowUpTarget,
            HttpContext.RequestAborted
        );

        var resource = new ClassifyAndEvaluateResource
        {
            Classification = result.Classification.Classification,
            RequiresResponse = result.Classification.RequiresResponse,
            ResponseText = result.Classification.ResponseText,
            ConsumesRedirect = result.Classification.ConsumesRedirect,
            AbandonCompetency = result.Classification.AbandonCompetency,
            StoreNote = result.Classification.StoreNote
        };

        if (result.Evaluation != null)
        {
            resource.CompetencyScore = result.Evaluation.CompetencyScore;
            resource.Rationale = result.Evaluation.Rationale;
            resource.FollowUpNeeded = result.Evaluation.FollowUpNeeded;
            resource.FollowUpTarget = result.Evaluation.FollowUpTarget;
            resource.FollowUpQuestion = result.Evaluation.FollowUpNeeded
                ? result.Evaluation.GetEffectiveFollowUpQuestion()
                : null;
        }

        return Ok(resource);
    }

    /// <summary>
    /// Records a skipped competency (e.g., candidate gave two off-topic responses).
    /// Sets competency_skipped = true. Does NOT run STAR evaluation.
    /// </summary>
    [HttpPost("{id}/runtime/skip-competency")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CompetencyResponseResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CompetencyResponseResource>> SkipCompetency(
        Guid id,
        [FromBody] SkipCompetencyRequest request)
    {
        var context = await _domainFacade.LoadInterviewRuntimeContextAsync(id);
        if (context == null)
            return NotFound("Interview has no template assigned or template not found.");

        var competency = context.Competencies.FirstOrDefault(c => c.Id == request.CompetencyId);
        if (competency == null)
            return NotFound($"Competency {request.CompetencyId} not found in this interview's role.");

        var result = await _domainFacade.ScoreAndRecordSkippedCompetencyAsync(
            id,
            competency,
            request.PrimaryQuestion,
            request.SkipReason
        );

        return Ok(InterviewMapper.ToResource(result));
    }

    /// <summary>
    /// Generates a question and streams TTS audio in a single request.
    /// Eliminates the separate generate-question + TTS round-trips.
    /// Returns the generated question text in a header and streams audio/mpeg in the body.
    /// </summary>
    [HttpPost("{id}/runtime/generate-question-audio")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task GenerateQuestionWithAudio(
        Guid id,
        [FromBody] GenerateQuestionRequest request)
    {
        var context = await _domainFacade.LoadInterviewRuntimeContextAsync(id);
        if (context == null)
        {
            Response.StatusCode = 404;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = "Interview has no template assigned or template not found." });
            return;
        }

        var competency = context.Competencies.FirstOrDefault(c => c.Id == request.CompetencyId);
        if (competency == null)
        {
            Response.StatusCode = 404;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = $"Competency {request.CompetencyId} not found." });
            return;
        }

        string systemPromptStatic;
        string? systemPromptInterviewPart;
        if (context.Agent != null)
        {
            var parts = _domainFacade.BuildInterviewSystemPromptParts(context);
            systemPromptStatic = parts.StaticPart;
            systemPromptInterviewPart = parts.InterviewPart;
        }
        else
        {
            systemPromptStatic = "";
            systemPromptInterviewPart = null;
        }

        var question = await _domainFacade.GeneratePrimaryQuestionAsync(
            systemPromptStatic,
            systemPromptInterviewPart,
            competency,
            context.Role.RoleName,
            context.Role.Industry,
            context.JobTitle,
            context.ApplicantName,
            includeTransition: request.IncludeTransition,
            previousCompetencyName: request.PreviousCompetencyName,
            cancellationToken: HttpContext.RequestAborted
        );

        if (string.IsNullOrWhiteSpace(question))
            question = competency.CanonicalExample ?? $"Tell me about your experience with {competency.Name}.";

        var voiceId = context.Agent?.ElevenlabsVoiceId ?? "21m00Tcm4TlvDq8ikWAM";

        Response.ContentType = "audio/mpeg";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Generated-Question", Uri.EscapeDataString(question));
        Response.Headers.Append("X-Competency-Id", competency.Id.ToString());
        Response.Headers.Append("Access-Control-Expose-Headers", "X-Generated-Question, X-Competency-Id");
        await Response.Body.FlushAsync(HttpContext.RequestAborted);

        try
        {
            await foreach (var chunk in _domainFacade.StreamVoiceAsync(voiceId, question, HttpContext.RequestAborted))
            {
                await Response.Body.WriteAsync(chunk, HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
            }
        }
        catch (Exception) when (Response.HasStarted)
        {
            Response.Body.Close();
        }
    }

    /// <summary>
    /// LATENCY-CRITICAL: Streaming conversation turn endpoint. Generates an AI response to the
    /// candidate's answer and streams TTS audio back in real time. Replaces the sequential
    /// classify → evaluate → TTS round trips with a single streaming pipeline.
    /// Response text and type are returned in headers; audio/mpeg streams in the body.
    /// </summary>
    [HttpPost("{id}/runtime/respond-to-turn")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task RespondToTurn(Guid id, [FromBody] RespondToTurnResource request)
    {
        var requestSw = Stopwatch.StartNew();

        var ctxSw = Stopwatch.StartNew();
        var context = await _domainFacade.LoadInterviewRuntimeContextAsync(id);
        ctxSw.Stop();

        if (context == null)
        {
            Response.StatusCode = 404;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = "Interview has no template assigned or template not found." });
            return;
        }

        var competency = context.Competencies.FirstOrDefault(c => c.Id == request.CompetencyId);
        if (competency == null)
        {
            Response.StatusCode = 404;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = $"Competency {request.CompetencyId} not found." });
            return;
        }

        Console.WriteLine($"[INTERVIEW][TIMING][API] Context load: {ctxSw.ElapsedMilliseconds}ms | interview={id}, competency={request.CompetencyName}");

        var domainRequest = new RespondToTurnRequest
        {
            CandidateTranscript = request.CandidateTranscript,
            CompetencyId = request.CompetencyId,
            CompetencyName = request.CompetencyName,
            CurrentQuestion = request.CurrentQuestion,
            Phase = request.Phase,
            FollowUpCount = request.FollowUpCount,
            AccumulatedTranscript = request.AccumulatedTranscript,
            PreviousFollowUpTarget = request.PreviousFollowUpTarget,
            RepeatsRemaining = request.RepeatsRemaining,
            Language = request.Language,
            PreviousAiResponse = request.PreviousAiResponse,
            IsLastCompetency = request.IsLastCompetency
        };

        Response.ContentType = "audio/mpeg";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Response-Type", "pending");
        Response.Headers.Append("Access-Control-Expose-Headers",
            "X-Response-Type, X-Follow-Up-Target, X-Language-Code");

        try
        {
            long firstChunkMs = 0;
            int totalChunks = 0;
            long totalBytes = 0;

            await foreach (var chunk in _domainFacade.RespondToTurnAsync(
                context, domainRequest, HttpContext.RequestAborted))
            {
                totalChunks++;
                totalBytes += chunk.Length;
                if (firstChunkMs == 0) firstChunkMs = requestSw.ElapsedMilliseconds;

                await Response.Body.WriteAsync(chunk, HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
            }

            requestSw.Stop();
            Console.WriteLine($"[INTERVIEW][TIMING][API] respond-to-turn complete: {requestSw.ElapsedMilliseconds}ms | ctx={ctxSw.ElapsedMilliseconds}ms, first_chunk={firstChunkMs}ms, chunks={totalChunks}, bytes={totalBytes}");
        }
        catch (Exception) when (Response.HasStarted)
        {
            requestSw.Stop();
            Console.WriteLine($"[INTERVIEW][TIMING][API] respond-to-turn aborted: {requestSw.ElapsedMilliseconds}ms");
            Response.Body.Close();
        }
    }

    /// <summary>
    /// Calculates the final interview score from recorded competency responses (weighted average).
    /// </summary>
    [HttpPost("{id}/runtime/finalize")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InterviewResultResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewResultResource>> FinalizeInterview(Guid id)
    {
        var result = await _domainFacade.ScoreInterviewFromCompetencyResponses(id);
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
