namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when unable to connect to Telnyx services
/// </summary>
public class TelnyxConnectionException : GatewayConnectionException
{
    public TelnyxConnectionException(string message) : base(message)
    {
    }

    public TelnyxConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
