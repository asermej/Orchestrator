namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Redeems an invite short code: validates the invite, creates a candidate session, and returns a JWT.
    /// </summary>
    public async Task<CandidateSessionResult> RedeemInterviewInvite(string shortCode, string? ipAddress, string? userAgent)
    {
        return await CandidateSessionManager.RedeemInvite(shortCode, ipAddress, userAgent).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates a candidate session JWT's jti against the database.
    /// Returns the session if valid, throws if not.
    /// </summary>
    public async Task<CandidateSession> ValidateCandidateSession(string jti)
    {
        return await CandidateSessionManager.ValidateSession(jti).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the candidate token signing secret from configuration.
    /// Used by API middleware to validate candidate JWTs.
    /// </summary>
    public string GetCandidateTokenSecret()
    {
        return CandidateSessionManager.GetCandidateTokenSecret();
    }
}
