namespace Orchestrator.Domain;

public class ChatValidationException : BusinessBaseException
{
    public override string Reason => "Chat validation failed";

    public ChatValidationException(string message) : base(message)
    {
    }

    public ChatValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

