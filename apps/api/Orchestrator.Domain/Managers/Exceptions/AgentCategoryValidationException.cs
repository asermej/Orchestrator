using System;

namespace Orchestrator.Domain;

public class AgentCategoryValidationException : BusinessBaseException
{
    public override string Reason => "AgentCategory validation failed";

    public AgentCategoryValidationException(string message) : base(message)
    {
    }

    public AgentCategoryValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

