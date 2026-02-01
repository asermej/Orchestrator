namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when gateway configuration is missing or invalid
/// </summary>
public class GatewayConfigurationException : TechnicalBaseException
{
    public override string Reason => "Gateway configuration error";

    public GatewayConfigurationException(string message) : base(message)
    {
    }

    public GatewayConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

