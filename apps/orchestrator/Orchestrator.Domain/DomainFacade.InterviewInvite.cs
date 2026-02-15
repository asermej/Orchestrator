namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Creates an invite for an interview with a short code for candidate access
    /// </summary>
    public async Task<InterviewInvite> CreateInterviewInvite(Guid interviewId, Guid organizationId, int maxUses = 3, int expiryDays = 7)
    {
        return await InterviewInviteManager.CreateInvite(interviewId, organizationId, maxUses, expiryDays).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an interview invite by ID
    /// </summary>
    public async Task<InterviewInvite?> GetInterviewInviteById(Guid id)
    {
        return await InterviewInviteManager.GetInviteById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an interview invite by its short code
    /// </summary>
    public async Task<InterviewInvite?> GetInterviewInviteByShortCode(string shortCode)
    {
        return await InterviewInviteManager.GetInviteByShortCode(shortCode).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an interview invite by interview ID
    /// </summary>
    public async Task<InterviewInvite?> GetInterviewInviteByInterviewId(Guid interviewId)
    {
        return await InterviewInviteManager.GetInviteByInterviewId(interviewId).ConfigureAwait(false);
    }

    /// <summary>
    /// Revokes an interview invite so it can no longer be used
    /// </summary>
    public async Task<InterviewInvite> RevokeInterviewInvite(Guid inviteId, string? revokedBy = null)
    {
        return await InterviewInviteManager.RevokeInvite(inviteId, revokedBy).ConfigureAwait(false);
    }

    /// <summary>
    /// Marks an interview invite as consumed
    /// </summary>
    public async Task<InterviewInvite> ConsumeInterviewInvite(Guid inviteId)
    {
        return await InterviewInviteManager.ConsumeInvite(inviteId).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an interview invite
    /// </summary>
    public async Task<bool> DeleteInterviewInvite(Guid id)
    {
        return await InterviewInviteManager.DeleteInvite(id).ConfigureAwait(false);
    }
}
