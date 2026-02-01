namespace Orchestrator.Domain;

public class MessageValidationException : BusinessBaseException
{
    public override string Reason => "Message validation failed";

    public MessageValidationException(string message) : base(message)
    {
    }

    public MessageValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

