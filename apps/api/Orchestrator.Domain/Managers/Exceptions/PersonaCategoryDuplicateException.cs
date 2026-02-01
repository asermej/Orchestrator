using System;

namespace Orchestrator.Domain;

public class PersonaCategoryDuplicateException : BusinessBaseException
{
    public override string Reason => "PersonaCategory already exists";

    public PersonaCategoryDuplicateException(string message) : base(message)
    {
    }

    public PersonaCategoryDuplicateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

