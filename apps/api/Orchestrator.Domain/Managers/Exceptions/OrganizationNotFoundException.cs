namespace Orchestrator.Domain;

public class OrganizationNotFoundException : NotFoundBaseException
{
    public override string Reason => "Organization not found";

    public OrganizationNotFoundException(string message) : base(message)
    {
    }

    public OrganizationNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
