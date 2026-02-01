using System;

namespace Orchestrator.Domain;

public class TopicNotFoundException : NotFoundBaseException
{
    public override string Reason => "Topic not found";

    public TopicNotFoundException(string message) : base(message)
    {
    }

    public TopicNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

