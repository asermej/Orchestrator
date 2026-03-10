namespace Orchestrator.Domain;

public class InterviewTemplateValidationException : BusinessBaseException
{
    public override string Reason => "Interview template validation failed";

    public InterviewTemplateValidationException(string message) : base(message)
    {
    }
}
