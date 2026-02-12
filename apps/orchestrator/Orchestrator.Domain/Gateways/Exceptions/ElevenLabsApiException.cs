namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when the ElevenLabs API returns an error response
/// </summary>
public class ElevenLabsApiException : GatewayApiException
{
    public ElevenLabsApiException(string message) : base(message)
    {
    }

    public ElevenLabsApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
