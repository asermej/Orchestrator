using System;

namespace Orchestrator.Domain;

public class PersonaCategoryValidationException : BusinessBaseException
{
    public override string Reason => "PersonaCategory validation failed";

    public PersonaCategoryValidationException(string message) : base(message)
    {
    }

    public PersonaCategoryValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

