namespace Orchestrator.Domain;

public class InterviewNotFoundException : NotFoundBaseException
{
    public override string Reason => "Interview not found";

    public InterviewNotFoundException(string message) : base(message)
    {
    }

    public InterviewNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
