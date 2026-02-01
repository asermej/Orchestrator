using System;

namespace Orchestrator.Domain;

public class PersonaCategoryNotFoundException : NotFoundBaseException
{
    public override string Reason => "PersonaCategory not found";

    public PersonaCategoryNotFoundException(string message) : base(message)
    {
    }

    public PersonaCategoryNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

