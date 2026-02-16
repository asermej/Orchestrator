using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

/// <summary>
/// Exception thrown when the Orchestrator API returns a non-success status code
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class OrchestratorApiException : TechnicalBaseException
{
    public override string Reason => "Orchestrator API error";

    public OrchestratorApiException(string message) : base(message)
    {
    }

    public OrchestratorApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
