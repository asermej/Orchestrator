namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the InterviewInvite entity.
/// </summary>
internal static class InterviewInviteValidator
{
    public static void Validate(InterviewInvite invite)
    {
        var errors = new List<string>();

        if (invite.InterviewId == Guid.Empty)
        {
            errors.Add("InterviewId is required.");
        }

        if (invite.GroupId == Guid.Empty)
        {
            errors.Add("GroupId is required.");
        }

        var shortCodeError = ValidatorString.Validate("ShortCode", invite.ShortCode);
        if (shortCodeError != null)
        {
            errors.Add(shortCodeError);
        }

        if (invite.ExpiresAt <= DateTime.UtcNow)
        {
            errors.Add("ExpiresAt must be in the future.");
        }

        if (invite.MaxUses < 1)
        {
            errors.Add("MaxUses must be at least 1.");
        }

        if (errors.Any())
        {
            throw new InviteValidationException(string.Join("; ", errors));
        }
    }
}
