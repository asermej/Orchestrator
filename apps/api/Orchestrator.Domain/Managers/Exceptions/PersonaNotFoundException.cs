namespace Orchestrator.Domain;

public class PersonaNotFoundException : NotFoundBaseException
{
    public override string Reason => "Persona not found";

    public PersonaNotFoundException(string message) : base(message)
    {
    }

    public PersonaNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

