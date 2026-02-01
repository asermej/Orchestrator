namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when voice clone rate limit is exceeded (e.g. one clone per user per 24h).
/// </summary>
public class VoiceCloneRateLimitExceededException : BusinessBaseException
{
    public override string Reason => Message;

    public VoiceCloneRateLimitExceededException()
        : base("Voice clone rate limit exceeded. Please try again later.")
    {
    }

    public VoiceCloneRateLimitExceededException(string message)
        : base(message)
    {
    }

    public VoiceCloneRateLimitExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
