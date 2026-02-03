namespace Orchestrator.Domain;

public class JobTypeNotFoundException : NotFoundBaseException
{
    public override string Reason => "Job type not found";

    public JobTypeNotFoundException(string message) : base(message)
    {
    }

    public JobTypeNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
