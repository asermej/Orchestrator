namespace Orchestrator.Domain;

public class AgentValidationException : BusinessBaseException
{
    public override string Reason => "Agent validation failed";

    public AgentValidationException(string message) : base(message)
    {
    }

    public AgentValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
