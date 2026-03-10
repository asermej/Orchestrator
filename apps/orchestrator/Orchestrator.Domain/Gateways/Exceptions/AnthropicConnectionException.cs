using System;

namespace Orchestrator.Domain;

internal sealed class AnthropicConnectionException : GatewayConnectionException
{
    public AnthropicConnectionException(string message) : base(message)
    {
    }

    public AnthropicConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
