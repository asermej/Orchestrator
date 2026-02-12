namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when connection to external service fails
/// </summary>
public class GatewayConnectionException : TechnicalBaseException
{
    public override string Reason => "Gateway connection error";

    public GatewayConnectionException(string message) : base(message)
    {
    }

    public GatewayConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

