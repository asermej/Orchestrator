namespace Orchestrator.Domain;

/// <summary>
/// Represents the result of an image download operation
/// </summary>
public class ImageDownloadResult
{
    /// <summary>
    /// The stream containing the image data
    /// </summary>
    public Stream Stream { get; set; } = Stream.Null;

    /// <summary>
    /// The content type (MIME type) of the image
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The key/filename of the image
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
