namespace Orchestrator.Domain;

public class AgentDuplicateDisplayNameException : BusinessBaseException
{
    public override string Reason => "Agent DisplayName already exists";

    public AgentDuplicateDisplayNameException(string message) : base(message)
    {
    }

    public AgentDuplicateDisplayNameException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
