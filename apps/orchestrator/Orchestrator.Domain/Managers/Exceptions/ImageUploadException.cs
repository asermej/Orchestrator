namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when image upload or storage operation fails
/// </summary>
public class ImageUploadException : BusinessBaseException
{
    public override string Reason => "Image upload failed";

    public ImageUploadException(string message) : base(message)
    {
    }

    public ImageUploadException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

