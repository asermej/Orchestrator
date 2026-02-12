using System;

namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when connection to OpenAI API fails
/// </summary>
internal sealed class OpenAIConnectionException : GatewayConnectionException
{
    public OpenAIConnectionException(string message) : base(message)
    {
    }

    public OpenAIConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

