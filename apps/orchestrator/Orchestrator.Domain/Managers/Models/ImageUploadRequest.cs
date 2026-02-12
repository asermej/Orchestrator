namespace Orchestrator.Domain;

/// <summary>
/// Represents a request to upload an image file
/// </summary>
public class ImageUploadRequest
{
    /// <summary>
    /// The original filename of the uploaded file
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The content type (MIME type) of the file
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The size of the file in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// The file stream containing the image data
    /// </summary>
    public Stream FileStream { get; set; } = Stream.Null;
}

