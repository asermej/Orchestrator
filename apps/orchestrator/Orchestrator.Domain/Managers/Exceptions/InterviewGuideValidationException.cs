namespace Orchestrator.Domain;

public class InterviewGuideValidationException : BusinessBaseException
{
    public override string Reason => "Interview guide validation failed";

    public InterviewGuideValidationException(string message) : base(message)
    {
    }

    public InterviewGuideValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
