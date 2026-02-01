namespace Orchestrator.Domain;

public class TagDuplicateException : BusinessBaseException
{
    public override string Reason => "Tag already exists";

    public TagDuplicateException(string message) : base(message)
    {
    }

    public TagDuplicateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

