using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for candidate-facing interview endpoints.
/// Uses candidate session JWT authentication (not Auth0).
/// </summary>
[ApiController]
[Route("api/v1/candidate")]
[Produces("application/json")]
public class CandidateController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public CandidateController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Redeems an invite short code and creates a candidate session.
    /// Returns a JWT token and interview data.
    /// </summary>
    [HttpPost("sessions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CandidateSessionResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(410)]
    public async Task<ActionResult<CandidateSessionResponse>> CreateSession([FromBody] RedeemInviteRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _domainFacade.RedeemInterviewInvite(request.ShortCode, ipAddress, userAgent);
        var response = CandidateMapper.ToSessionResponse(result);

        return Ok(response);
    }

    /// <summary>
    /// Gets the interview detail for the current candidate session.
    /// </summary>
    [HttpGet("interview")]
    [ProducesResponseType(typeof(CandidateInterviewResource), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CandidateInterviewResource>> GetInterview()
    {
        var interviewId = GetInterviewIdFromContext();
        var interview = await _domainFacade.GetInterviewById(interviewId);
        if (interview == null)
        {
            return NotFound("Interview not found");
        }

        return Ok(CandidateMapper.ToInterviewResource(interview));
    }

    /// <summary>
    /// Starts the interview for the current candidate session.
    /// </summary>
    [HttpPost("interview/start")]
    [ProducesResponseType(typeof(CandidateInterviewResource), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CandidateInterviewResource>> StartInterview()
    {
        var interviewId = GetInterviewIdFromContext();
        var interview = await _domainFacade.StartInterview(interviewId);
        return Ok(CandidateMapper.ToInterviewResource(interview));
    }

    /// <summary>
    /// Adds a response to the interview for the current candidate session.
    /// </summary>
    [HttpPost("interview/responses")]
    [ProducesResponseType(typeof(InterviewResponseResource), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewResponseResource>> AddResponse([FromBody] CreateInterviewResponseResource resource)
    {
        var interviewId = GetInterviewIdFromContext();

        var response = InterviewMapper.ToResponseDomain(resource, interviewId);

        if (resource.IsFollowUp)
        {
            response.QuestionType = "followup";
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
        return Created($"/api/v1/candidate/interview/responses/{created.Id}", InterviewMapper.ToResponseResource(created));
    }

    /// <summary>
    /// Completes the interview for the current candidate session.
    /// Also marks the invite as consumed.
    /// </summary>
    [HttpPost("interview/complete")]
    [ProducesResponseType(typeof(CandidateInterviewResource), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CandidateInterviewResource>> CompleteInterview()
    {
        var interviewId = GetInterviewIdFromContext();
        var inviteId = GetInviteIdFromContext();

        var interview = await _domainFacade.CompleteInterview(interviewId);

        // Mark the invite as consumed
        if (inviteId != Guid.Empty)
        {
            try
            {
                await _domainFacade.ConsumeInterviewInvite(inviteId);
            }
            catch (Exception ex)
            {
                // Log but don't fail the completion
                System.Diagnostics.Debug.WriteLine($"Failed to consume invite {inviteId}: {ex.Message}");
            }
        }

        return Ok(CandidateMapper.ToInterviewResource(interview));
    }

    /// <summary>
    /// Uploads interview audio recording for the current candidate session.
    /// </summary>
    [HttpPost("interview/audio/upload")]
    [ProducesResponseType(typeof(AudioUploadResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [RequestSizeLimit(52428800)] // 50MB
    public async Task<ActionResult<AudioUploadResponse>> UploadAudio(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No audio file uploaded" });
        }

        using var stream = file.OpenReadStream();
        var url = await _domainFacade.UploadInterviewAudioAsync(stream, file.ContentType);
        return Ok(new AudioUploadResponse { Url = url });
    }

    /// <summary>
    /// Warms up audio cache for interview questions (candidate session auth).
    /// </summary>
    [HttpPost("interview/audio/warmup")]
    [ProducesResponseType(typeof(InterviewAudioWarmupResource), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InterviewAudioWarmupResource>> WarmupAudio()
    {
        var interviewId = GetInterviewIdFromContext();

        var result = await _domainFacade.WarmupInterviewAudioAsync(interviewId, HttpContext.RequestAborted);

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

    private Guid GetInterviewIdFromContext()
    {
        if (HttpContext.Items.TryGetValue("InterviewId", out var idObj) && idObj is Guid id)
        {
            return id;
        }
        throw new UnauthorizedAccessException("Interview ID not found in session context");
    }

    private Guid GetInviteIdFromContext()
    {
        if (HttpContext.Items.TryGetValue("InviteId", out var idObj) && idObj is Guid id)
        {
            return id;
        }
        return Guid.Empty;
    }
}
