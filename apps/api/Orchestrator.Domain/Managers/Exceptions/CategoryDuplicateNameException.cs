using System;

namespace Orchestrator.Domain;

public class CategoryDuplicateNameException : BusinessBaseException
{
    public override string Reason => "Category name already exists";

    public CategoryDuplicateNameException(string message) : base(message)
    {
    }

    public CategoryDuplicateNameException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

