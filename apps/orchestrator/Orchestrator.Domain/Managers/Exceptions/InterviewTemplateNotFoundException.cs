namespace Orchestrator.Domain;

public class InterviewTemplateNotFoundException : NotFoundBaseException
{
    public override string Reason => "Interview template not found";

    public InterviewTemplateNotFoundException(string message) : base(message)
    {
    }
}
