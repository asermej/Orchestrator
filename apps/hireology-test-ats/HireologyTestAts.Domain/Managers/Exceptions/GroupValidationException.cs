using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class GroupValidationException : BusinessBaseException
{
    public override string Reason { get; }

    public GroupValidationException(string reason) : base(reason)
    {
        Reason = reason;
    }
}
