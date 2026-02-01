namespace Orchestrator.Domain;

public class PersonaDuplicateDisplayNameException : BusinessBaseException
{
    public override string Reason => "Persona DisplayName already exists";

    public PersonaDuplicateDisplayNameException(string message) : base(message)
    {
    }

    public PersonaDuplicateDisplayNameException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

