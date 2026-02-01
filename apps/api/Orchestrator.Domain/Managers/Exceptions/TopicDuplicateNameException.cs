using System;

namespace Orchestrator.Domain;

public class TopicDuplicateNameException : BusinessBaseException
{
    public override string Reason => "Topic Name already exists";

    public TopicDuplicateNameException(string message) : base(message)
    {
    }

    public TopicDuplicateNameException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

