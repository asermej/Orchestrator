using System;

namespace Orchestrator.Domain;

public class AgentCategoryNotFoundException : NotFoundBaseException
{
    public override string Reason => "AgentCategory not found";

    public AgentCategoryNotFoundException(string message) : base(message)
    {
    }

    public AgentCategoryNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

