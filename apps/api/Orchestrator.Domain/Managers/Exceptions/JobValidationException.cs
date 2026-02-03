namespace Orchestrator.Domain;

public class JobValidationException : BusinessBaseException
{
    public override string Reason => "Job validation failed";

    public JobValidationException(string message) : base(message)
    {
    }

    public JobValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
