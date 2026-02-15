namespace Orchestrator.Domain;

public class CandidateSessionExpiredException : BusinessBaseException
{
    public override string Reason => "Candidate session has expired";

    public CandidateSessionExpiredException(string message) : base(message)
    {
    }

    public CandidateSessionExpiredException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
