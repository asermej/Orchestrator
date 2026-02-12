using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class OrganizationValidationException : BusinessBaseException
{
    public override string Reason { get; }

    public OrganizationValidationException(string reason) : base(reason)
    {
        Reason = reason;
    }
}
