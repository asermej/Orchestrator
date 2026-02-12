namespace Orchestrator.Domain;

public class InterviewConfigurationNotFoundException : NotFoundBaseException
{
    public override string Reason => "Interview configuration not found";

    public InterviewConfigurationNotFoundException(string message) : base(message)
    {
    }

    public InterviewConfigurationNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
