using System;

namespace Orchestrator.Domain;

internal sealed class AnthropicApiException : GatewayApiException
{
    public AnthropicApiException(string message) : base(message)
    {
    }

    public AnthropicApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
