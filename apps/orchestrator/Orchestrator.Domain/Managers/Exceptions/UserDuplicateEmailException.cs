namespace Orchestrator.Domain;

public class UserDuplicateEmailException : BusinessBaseException
{
    public override string Reason => "User Email already exists";

    public UserDuplicateEmailException(string message) : base(message)
    {
    }

    public UserDuplicateEmailException(string message, Exception innerException) : base(message, innerException)
    {
    }
} 