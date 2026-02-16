using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Orchestrator.Domain;

/// <summary>
/// Manages candidate session creation, JWT generation, and session validation.
/// </summary>
internal sealed class CandidateSessionManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    private const int SessionDurationHours = 2;

    public CandidateSessionManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    /// <summary>
    /// Redeems an invite short code: validates the invite, creates a session, and returns a JWT.
    /// </summary>
    public async Task<CandidateSessionResult> RedeemInvite(string shortCode, string? ipAddress, string? userAgent)
    {
        var invite = await DataFacade.GetInterviewInviteByShortCode(shortCode).ConfigureAwait(false);
        if (invite == null)
        {
            throw new InviteNotFoundException($"Interview invite with short code '{shortCode}' not found.");
        }

        // Validate invite state
        if (invite.Status != InviteStatus.Active)
        {
            throw new InviteNotActiveException($"Interview invite is '{invite.Status}', not active.");
        }

        if (invite.ExpiresAt < DateTime.UtcNow)
        {
            throw new InviteExpiredException($"Interview invite expired at {invite.ExpiresAt:u}.");
        }

        if (invite.UseCount >= invite.MaxUses)
        {
            throw new InviteMaxUsesExceededException($"Interview invite has reached its maximum of {invite.MaxUses} uses.");
        }

        // Deactivate any previous sessions for this invite
        await DataFacade.DeactivatePreviousCandidateSessions(invite.Id).ConfigureAwait(false);

        // Create a new session
        var jti = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddHours(SessionDurationHours);

        var session = new CandidateSession
        {
            InviteId = invite.Id,
            InterviewId = invite.InterviewId,
            Jti = jti,
            IsActive = true,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            StartedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
        };
        var createdSession = await DataFacade.AddCandidateSession(session).ConfigureAwait(false);

        // Increment use count on invite
        invite.UseCount++;
        await DataFacade.UpdateInterviewInvite(invite).ConfigureAwait(false);

        // Generate JWT
        var token = GenerateCandidateJwt(invite, createdSession, expiresAt);

        // Load interview detail
        var interview = await DataFacade.GetInterviewById(invite.InterviewId).ConfigureAwait(false);

        // Load related data
        Agent? agent = null;
        Job? job = null;
        Applicant? applicant = null;
        List<InterviewConfigurationQuestion> questions = new();
        if (interview != null)
        {
            agent = await DataFacade.GetAgentById(interview.AgentId).ConfigureAwait(false);
            job = await DataFacade.GetJobById(interview.JobId).ConfigureAwait(false);
            applicant = await DataFacade.GetApplicantById(interview.ApplicantId).ConfigureAwait(false);

            // Load questions from interview configuration
            if (interview.InterviewConfigurationId.HasValue)
            {
                questions = await DataFacade.GetInterviewConfigurationQuestions(interview.InterviewConfigurationId.Value).ConfigureAwait(false);
            }
        }

        // Log the redemption
        await DataFacade.AddInterviewAuditLog(new InterviewAuditLog
        {
            InterviewId = invite.InterviewId,
            InviteId = invite.Id,
            SessionId = createdSession.Id,
            EventType = AuditEventType.InviteRedeemed,
            IpAddress = ipAddress,
            UserAgent = userAgent,
        }).ConfigureAwait(false);

        return new CandidateSessionResult
        {
            Token = token,
            Interview = interview!,
            Agent = agent,
            Job = job,
            Applicant = applicant,
            Questions = questions,
            Session = createdSession,
        };
    }

    /// <summary>
    /// Validates a candidate session JWT's jti against the database.
    /// Returns the session if valid, throws if not.
    /// </summary>
    public async Task<CandidateSession> ValidateSession(string jti)
    {
        var session = await DataFacade.GetCandidateSessionByJti(jti).ConfigureAwait(false);
        if (session == null)
        {
            throw new CandidateSessionNotFoundException($"Candidate session with jti '{jti}' not found.");
        }

        if (!session.IsActive)
        {
            throw new CandidateSessionExpiredException("Candidate session is no longer active.");
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            throw new CandidateSessionExpiredException($"Candidate session expired at {session.ExpiresAt:u}.");
        }

        // Update last activity
        session.LastActivityAt = DateTime.UtcNow;
        await DataFacade.UpdateCandidateSession(session).ConfigureAwait(false);

        return session;
    }

    /// <summary>
    /// Gets the candidate token secret from configuration.
    /// </summary>
    public string GetCandidateTokenSecret()
    {
        return _serviceLocator.CreateConfigurationProvider().GetCandidateTokenSecret();
    }

    private string GenerateCandidateJwt(InterviewInvite invite, CandidateSession session, DateTime expiresAt)
    {
        var secret = _serviceLocator.CreateConfigurationProvider().GetCandidateTokenSecret();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", $"candidate:{invite.Id}"),
            new Claim("interview_id", invite.InterviewId.ToString()),
            new Claim("invite_id", invite.Id.ToString()),
            new Claim("group_id", invite.GroupId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, session.Jti),
        };

        var token = new JwtSecurityToken(
            issuer: "hireology-candidate",
            audience: "hireology-api",
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}
