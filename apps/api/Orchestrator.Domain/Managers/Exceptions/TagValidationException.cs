namespace Orchestrator.Domain;

public class TagValidationException : BusinessBaseException
{
    public override string Reason => "Tag validation failed";

    public TagValidationException(string message) : base(message)
    {
    }

    public TagValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

