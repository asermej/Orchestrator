using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class ApplicantValidationException : BusinessBaseException
{
    public override string Reason { get; }

    public ApplicantValidationException(string reason) : base(reason)
    {
        Reason = reason;
    }
}
