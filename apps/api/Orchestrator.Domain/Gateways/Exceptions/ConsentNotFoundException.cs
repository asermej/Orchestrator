namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when a consent audit record is not found or does not match.
/// </summary>
public class ConsentNotFoundException : NotFoundBaseException
{
    public override string Reason => "Consent record not found or does not match.";

    public ConsentNotFoundException()
        : base("Consent record not found or does not match.")
    {
    }

    public ConsentNotFoundException(string message)
        : base(message)
    {
    }

    public ConsentNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
