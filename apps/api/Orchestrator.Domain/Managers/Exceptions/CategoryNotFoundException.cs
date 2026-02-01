using System;

namespace Orchestrator.Domain;

public class CategoryNotFoundException : NotFoundBaseException
{
    public override string Reason => "Category not found";

    public CategoryNotFoundException(string message) : base(message)
    {
    }

    public CategoryNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

