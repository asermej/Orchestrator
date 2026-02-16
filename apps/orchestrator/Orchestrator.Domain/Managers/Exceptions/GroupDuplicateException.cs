namespace Orchestrator.Domain;

public class GroupDuplicateException : BusinessBaseException
{
    public override string Reason => "Group already exists";

    public GroupDuplicateException(string message) : base(message)
    {
    }

    public GroupDuplicateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
