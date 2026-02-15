using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class InterviewRequestValidationException : BusinessBaseException
{
    public override string Reason { get; }

    public InterviewRequestValidationException(string reason) : base(reason)
    {
        Reason = reason;
    }
}
