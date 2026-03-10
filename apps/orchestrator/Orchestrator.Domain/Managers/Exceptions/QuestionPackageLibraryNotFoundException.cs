namespace Orchestrator.Domain;

public class QuestionPackageLibraryNotFoundException : NotFoundBaseException
{
    public override string Reason => "Question package library resource not found";

    public QuestionPackageLibraryNotFoundException(string message) : base(message)
    {
    }

    public QuestionPackageLibraryNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
