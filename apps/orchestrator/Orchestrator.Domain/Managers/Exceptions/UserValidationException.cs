namespace Orchestrator.Domain;

public class UserValidationException : BusinessBaseException
{
    public override string Reason => "User validation failed";

    public UserValidationException(string message) : base(message)
    {
    }

    public UserValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
} 