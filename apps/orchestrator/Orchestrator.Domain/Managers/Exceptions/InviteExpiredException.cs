namespace Orchestrator.Domain;

public class InviteExpiredException : BusinessBaseException
{
    public override string Reason => "Interview invite has expired";

    public InviteExpiredException(string message) : base(message)
    {
    }

    public InviteExpiredException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
