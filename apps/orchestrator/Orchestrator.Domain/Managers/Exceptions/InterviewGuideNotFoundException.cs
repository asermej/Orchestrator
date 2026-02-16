namespace Orchestrator.Domain;

public class InterviewGuideNotFoundException : NotFoundBaseException
{
    public override string Reason => "Interview guide not found";

    public InterviewGuideNotFoundException(string message) : base(message)
    {
    }

    public InterviewGuideNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
