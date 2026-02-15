namespace Orchestrator.Domain;

public class InviteNotActiveException : BusinessBaseException
{
    public override string Reason => "Interview invite is not active";

    public InviteNotActiveException(string message) : base(message)
    {
    }

    public InviteNotActiveException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
