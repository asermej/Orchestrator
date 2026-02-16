namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when an ATS API call fails
/// </summary>
public class AtsApiException : GatewayApiException
{
    public override string Reason => "ATS API error";

    public AtsApiException(string message) : base(message)
    {
    }

    public AtsApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
