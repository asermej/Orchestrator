namespace Orchestrator.Domain;

public class CandidateSessionNotFoundException : NotFoundBaseException
{
    public override string Reason => "Candidate session not found";

    public CandidateSessionNotFoundException(string message) : base(message)
    {
    }

    public CandidateSessionNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
