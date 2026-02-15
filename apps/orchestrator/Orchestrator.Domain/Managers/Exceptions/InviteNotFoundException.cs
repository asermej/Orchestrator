namespace Orchestrator.Domain;

public class InviteNotFoundException : NotFoundBaseException
{
    public override string Reason => "Interview invite not found";

    public InviteNotFoundException(string message) : base(message)
    {
    }

    public InviteNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
