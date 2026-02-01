namespace Orchestrator.Domain;

public class ChatNotFoundException : NotFoundBaseException
{
    public override string Reason => "Chat not found";

    public ChatNotFoundException(string message) : base(message)
    {
    }

    public ChatNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

