namespace Orchestrator.Domain;

public class InterviewConfigurationValidationException : BusinessBaseException
{
    public override string Reason => "Interview configuration validation failed";

    public InterviewConfigurationValidationException(string message) : base(message)
    {
    }

    public InterviewConfigurationValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
