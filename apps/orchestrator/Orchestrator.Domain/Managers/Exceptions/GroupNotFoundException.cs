namespace Orchestrator.Domain;

public class GroupNotFoundException : NotFoundBaseException
{
    public override string Reason => "Group not found";

    public GroupNotFoundException(string message) : base(message)
    {
    }

    public GroupNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
