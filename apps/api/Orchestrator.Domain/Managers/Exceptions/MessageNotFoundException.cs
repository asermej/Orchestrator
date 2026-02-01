namespace Orchestrator.Domain;

public class MessageNotFoundException : NotFoundBaseException
{
    public override string Reason => "Message not found";

    public MessageNotFoundException(string message) : base(message)
    {
    }

    public MessageNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

