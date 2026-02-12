namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when an external API call fails
/// </summary>
public class GatewayApiException : TechnicalBaseException
{
    public override string Reason => "Gateway API error";

    public GatewayApiException(string message) : base(message)
    {
    }

    public GatewayApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

