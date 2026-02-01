namespace Orchestrator.Domain;

public class MessageDuplicateException : BusinessBaseException
{
    public override string Reason => "Message already exists";

    public MessageDuplicateException(string message) : base(message)
    {
    }

    public MessageDuplicateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

