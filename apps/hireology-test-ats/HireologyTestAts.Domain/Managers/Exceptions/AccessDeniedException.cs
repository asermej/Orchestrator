using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class AccessDeniedException : BusinessBaseException
{
    public override string Reason { get; }

    public AccessDeniedException(string reason) : base(reason)
    {
        Reason = reason;
    }

    public AccessDeniedException() : base("Access denied")
    {
        Reason = "Access denied";
    }
}
