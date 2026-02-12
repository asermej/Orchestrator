namespace Orchestrator.Domain;

public class ApplicantValidationException : BusinessBaseException
{
    public override string Reason => "Applicant validation failed";

    public ApplicantValidationException(string message) : base(message)
    {
    }

    public ApplicantValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
