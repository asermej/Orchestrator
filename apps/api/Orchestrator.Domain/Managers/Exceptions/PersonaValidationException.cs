namespace Orchestrator.Domain;

public class PersonaValidationException : BusinessBaseException
{
    public override string Reason => "Persona validation failed";

    public PersonaValidationException(string message) : base(message)
    {
    }

    public PersonaValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

