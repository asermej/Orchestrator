using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class ApplicantNotFoundException : NotFoundBaseException
{
    public override string Reason => "Applicant not found";

    public ApplicantNotFoundException() : base("Applicant not found")
    {
    }

    public ApplicantNotFoundException(string message) : base(message)
    {
    }
}
