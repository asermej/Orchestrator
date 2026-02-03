using System;

namespace Orchestrator.Domain;

public class AgentCategoryDuplicateException : BusinessBaseException
{
    public override string Reason => "AgentCategory already exists";

    public AgentCategoryDuplicateException(string message) : base(message)
    {
    }

    public AgentCategoryDuplicateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

