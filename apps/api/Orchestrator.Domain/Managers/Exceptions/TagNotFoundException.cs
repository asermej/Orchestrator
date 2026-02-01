namespace Orchestrator.Domain;

public class TagNotFoundException : NotFoundBaseException
{
    public override string Reason => "Tag not found";

    public TagNotFoundException(string message) : base(message)
    {
    }

    public TagNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

