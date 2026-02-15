using System.Security.Cryptography;

namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for InterviewInvite entities
/// </summary>
internal sealed class InterviewInviteManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public InterviewInviteManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    /// <summary>
    /// Creates an invite for an interview. Resolves the organization via the job.
    /// </summary>
    public async Task<InterviewInvite> CreateInvite(Guid interviewId, Guid organizationId, int maxUses = 3, int expiryDays = 7)
    {
        var invite = new InterviewInvite
        {
            InterviewId = interviewId,
            OrganizationId = organizationId,
            ShortCode = GenerateShortCode(),
            Status = InviteStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            MaxUses = maxUses,
        };

        InterviewInviteValidator.Validate(invite);
        return await DataFacade.AddInterviewInvite(invite).ConfigureAwait(false);
    }

    public async Task<InterviewInvite?> GetInviteById(Guid id)
    {
        return await DataFacade.GetInterviewInviteById(id).ConfigureAwait(false);
    }

    public async Task<InterviewInvite?> GetInviteByShortCode(string shortCode)
    {
        return await DataFacade.GetInterviewInviteByShortCode(shortCode).ConfigureAwait(false);
    }

    public async Task<InterviewInvite?> GetInviteByInterviewId(Guid interviewId)
    {
        return await DataFacade.GetInterviewInviteByInterviewId(interviewId).ConfigureAwait(false);
    }

    public async Task<InterviewInvite> UpdateInvite(InterviewInvite invite)
    {
        return await DataFacade.UpdateInterviewInvite(invite).ConfigureAwait(false);
    }

    /// <summary>
    /// Revokes an invite so it can no longer be used
    /// </summary>
    public async Task<InterviewInvite> RevokeInvite(Guid inviteId, string? revokedBy = null)
    {
        var invite = await DataFacade.GetInterviewInviteById(inviteId).ConfigureAwait(false);
        if (invite == null)
        {
            throw new InviteNotFoundException($"Interview invite with ID {inviteId} not found.");
        }

        invite.Status = InviteStatus.Revoked;
        invite.RevokedAt = DateTime.UtcNow;
        invite.RevokedBy = revokedBy;

        return await DataFacade.UpdateInterviewInvite(invite).ConfigureAwait(false);
    }

    /// <summary>
    /// Marks an invite as consumed (typically after interview completion)
    /// </summary>
    public async Task<InterviewInvite> ConsumeInvite(Guid inviteId)
    {
        var invite = await DataFacade.GetInterviewInviteById(inviteId).ConfigureAwait(false);
        if (invite == null)
        {
            throw new InviteNotFoundException($"Interview invite with ID {inviteId} not found.");
        }

        invite.Status = InviteStatus.Consumed;

        return await DataFacade.UpdateInterviewInvite(invite).ConfigureAwait(false);
    }

    public async Task<bool> DeleteInvite(Guid id)
    {
        return await DataFacade.DeleteInterviewInvite(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates a 12-character URL-safe short code using CSPRNG
    /// </summary>
    internal static string GenerateShortCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = new byte[12];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}
