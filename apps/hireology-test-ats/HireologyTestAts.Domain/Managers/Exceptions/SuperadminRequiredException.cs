using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class SuperadminRequiredException : BusinessBaseException
{
    public override string Reason { get; }

    public SuperadminRequiredException() : base("Superadmin privileges required")
    {
        Reason = "Superadmin privileges required";
    }

    public SuperadminRequiredException(string reason) : base(reason)
    {
        Reason = reason;
    }
}
