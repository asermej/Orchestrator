namespace Orchestrator.Domain;

public class UserNotFoundException : NotFoundBaseException
{
    public override string Reason => "User not found";

    public UserNotFoundException(string message) : base(message)
    {
    }

    public UserNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
} 