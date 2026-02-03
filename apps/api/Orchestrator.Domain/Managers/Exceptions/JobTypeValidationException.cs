namespace Orchestrator.Domain;

public class JobTypeValidationException : BusinessBaseException
{
    public override string Reason => "Job type validation failed";

    public JobTypeValidationException(string message) : base(message)
    {
    }

    public JobTypeValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
