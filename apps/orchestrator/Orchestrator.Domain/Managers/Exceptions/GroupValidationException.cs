namespace Orchestrator.Domain;

public class GroupValidationException : BusinessBaseException
{
    public override string Reason => "Group validation failed";

    public GroupValidationException(string message) : base(message)
    {
    }

    public GroupValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
