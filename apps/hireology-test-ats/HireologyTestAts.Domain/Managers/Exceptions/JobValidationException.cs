using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class JobValidationException : BusinessBaseException
{
    public override string Reason { get; }

    public JobValidationException(string reason) : base(reason)
    {
        Reason = reason;
    }
}
