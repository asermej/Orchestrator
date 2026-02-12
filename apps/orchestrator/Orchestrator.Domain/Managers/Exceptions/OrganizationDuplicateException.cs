namespace Orchestrator.Domain;

public class OrganizationDuplicateException : BusinessBaseException
{
    public override string Reason => "Organization already exists";

    public OrganizationDuplicateException(string message) : base(message)
    {
    }

    public OrganizationDuplicateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
