namespace Orchestrator.Domain;

public class JobNotFoundException : NotFoundBaseException
{
    public override string Reason => "Job not found";

    public JobNotFoundException(string message) : base(message)
    {
    }

    public JobNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
