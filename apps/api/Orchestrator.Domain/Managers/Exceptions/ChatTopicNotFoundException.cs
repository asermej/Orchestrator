using System;

namespace Orchestrator.Domain;

public class ChatTopicNotFoundException : NotFoundBaseException
{
    public override string Reason => "ChatTopic not found";

    public ChatTopicNotFoundException(string message) : base(message)
    {
    }

    public ChatTopicNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

