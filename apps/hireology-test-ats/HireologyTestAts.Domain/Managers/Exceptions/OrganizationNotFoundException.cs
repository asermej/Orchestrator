using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class OrganizationNotFoundException : NotFoundBaseException
{
    public override string Reason => "Organization not found";

    public OrganizationNotFoundException() : base("Organization not found")
    {
    }

    public OrganizationNotFoundException(string message) : base(message)
    {
    }
}
