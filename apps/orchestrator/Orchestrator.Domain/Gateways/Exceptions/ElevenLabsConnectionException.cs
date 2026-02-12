namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when unable to connect to the ElevenLabs API
/// </summary>
public class ElevenLabsConnectionException : GatewayConnectionException
{
    public ElevenLabsConnectionException(string message) : base(message)
    {
    }

    public ElevenLabsConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
