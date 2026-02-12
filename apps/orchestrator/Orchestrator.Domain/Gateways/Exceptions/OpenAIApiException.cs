using System;

namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when OpenAI API returns an error response
/// </summary>
internal sealed class OpenAIApiException : GatewayApiException
{
    public OpenAIApiException(string message) : base(message)
    {
    }

    public OpenAIApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

