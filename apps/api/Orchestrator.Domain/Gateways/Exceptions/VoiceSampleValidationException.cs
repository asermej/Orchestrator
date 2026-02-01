namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when voice sample validation fails (duration, size, etc.).
/// </summary>
public class VoiceSampleValidationException : BusinessBaseException
{
    public override string Reason => Message;

    public VoiceSampleValidationException()
        : base("Voice sample validation failed.")
    {
    }

    public VoiceSampleValidationException(string message)
        : base(message)
    {
    }

    public VoiceSampleValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
