namespace Orchestrator.Domain;

public class InviteValidationException : BusinessBaseException
{
    public override string Reason => "Interview invite validation failed";

    public InviteValidationException(string message) : base(message)
    {
    }

    public InviteValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
