namespace Orchestrator.Domain;

public class InterviewValidationException : BusinessBaseException
{
    public override string Reason => "Interview validation failed";

    public InterviewValidationException(string message) : base(message)
    {
    }

    public InterviewValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
