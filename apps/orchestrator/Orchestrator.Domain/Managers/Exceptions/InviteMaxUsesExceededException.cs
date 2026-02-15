namespace Orchestrator.Domain;

public class InviteMaxUsesExceededException : BusinessBaseException
{
    public override string Reason => "Interview invite has reached maximum uses";

    public InviteMaxUsesExceededException(string message) : base(message)
    {
    }

    public InviteMaxUsesExceededException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
