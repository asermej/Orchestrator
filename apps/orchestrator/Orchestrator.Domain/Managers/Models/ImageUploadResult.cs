namespace Orchestrator.Domain;

/// <summary>
/// Represents the result of an image upload operation
/// </summary>
public class ImageUploadResult
{
    /// <summary>
    /// The URL where the uploaded image can be accessed
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The unique filename that was generated for storage
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the image was uploaded
    /// </summary>
    public DateTime UploadedAt { get; set; }
}

