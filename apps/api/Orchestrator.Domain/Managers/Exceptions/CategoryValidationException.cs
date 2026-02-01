using System;

namespace Orchestrator.Domain;

public class CategoryValidationException : BusinessBaseException
{
    public override string Reason => "Category validation failed";

    public CategoryValidationException(string message) : base(message)
    {
    }

    public CategoryValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

