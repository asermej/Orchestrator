namespace Orchestrator.Domain;

public class OrganizationValidationException : BusinessBaseException
{
    public override string Reason => "Organization validation failed";

    public OrganizationValidationException(string message) : base(message)
    {
    }

    public OrganizationValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
