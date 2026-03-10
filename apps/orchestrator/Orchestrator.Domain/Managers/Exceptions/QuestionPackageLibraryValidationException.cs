namespace Orchestrator.Domain;

public class QuestionPackageLibraryValidationException : BusinessBaseException
{
    public override string Reason => "Question package library validation failed";

    public QuestionPackageLibraryValidationException(string message) : base(message)
    {
    }

    public QuestionPackageLibraryValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
