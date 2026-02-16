namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when connection to the ATS fails
/// </summary>
public class AtsConnectionException : GatewayConnectionException
{
    public override string Reason => "ATS connection error";

    public AtsConnectionException(string message) : base(message)
    {
    }

    public AtsConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
