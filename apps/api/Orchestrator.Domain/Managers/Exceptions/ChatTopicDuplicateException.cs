using System;

namespace Orchestrator.Domain;

public class ChatTopicDuplicateException : BusinessBaseException
{
    public override string Reason => "ChatTopic already exists for this chat and topic combination";

    public ChatTopicDuplicateException(string message) : base(message)
    {
    }

    public ChatTopicDuplicateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

