using System;

namespace Orchestrator.Domain;

public class TopicValidationException : BusinessBaseException
{
    public override string Reason => "Topic validation failed";

    public TopicValidationException(string message) : base(message)
    {
    }

    public TopicValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

