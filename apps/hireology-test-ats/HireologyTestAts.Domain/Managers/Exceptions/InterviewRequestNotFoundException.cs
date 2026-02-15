using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class InterviewRequestNotFoundException : NotFoundBaseException
{
    public override string Reason => "Interview request not found";

    public InterviewRequestNotFoundException() : base("Interview request not found")
    {
    }

    public InterviewRequestNotFoundException(string message) : base(message)
    {
    }
}
