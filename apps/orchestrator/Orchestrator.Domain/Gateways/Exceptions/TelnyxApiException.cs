namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when the Telnyx API or media stream returns an error
/// </summary>
public class TelnyxApiException : GatewayApiException
{
    public TelnyxApiException(string message) : base(message)
    {
    }

    public TelnyxApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
