namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when image validation fails
/// </summary>
public class ImageValidationException : BusinessBaseException
{
    public override string Reason => "Image validation failed";

    public ImageValidationException(string message) : base(message)
    {
    }

    public ImageValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

