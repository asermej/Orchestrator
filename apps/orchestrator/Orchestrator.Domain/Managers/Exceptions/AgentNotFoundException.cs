namespace Orchestrator.Domain;

public class AgentNotFoundException : NotFoundBaseException
{
    public override string Reason => "Agent not found";

    public AgentNotFoundException(string message) : base(message)
    {
    }

    public AgentNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
