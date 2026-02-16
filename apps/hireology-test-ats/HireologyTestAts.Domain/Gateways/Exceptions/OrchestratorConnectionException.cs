using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

/// <summary>
/// Exception thrown when a connection to the Orchestrator API fails or times out
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class OrchestratorConnectionException : TechnicalBaseException
{
    public override string Reason => "Orchestrator connection error";

    public OrchestratorConnectionException(string message) : base(message)
    {
    }

    public OrchestratorConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
