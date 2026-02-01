using System;

namespace Orchestrator.Domain;

public class ChatTopicValidationException : BusinessBaseException
{
    public override string Reason => Message ?? "ChatTopic validation failed";

    public ChatTopicValidationException(string message) : base(message)
    {
    }

    public ChatTopicValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

