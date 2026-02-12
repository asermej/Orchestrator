namespace Orchestrator.Domain;

public class ApplicantNotFoundException : NotFoundBaseException
{
    public override string Reason => "Applicant not found";

    public ApplicantNotFoundException(string message) : base(message)
    {
    }

    public ApplicantNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
