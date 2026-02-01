namespace Orchestrator.Domain;

public class ChatDuplicateException : BusinessBaseException
{
    public override string Reason => "Chat already exists";

    public ChatDuplicateException(string message) : base(message)
    {
    }

    public ChatDuplicateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

