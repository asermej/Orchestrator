using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for image upload and retrieval operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ImageController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public ImageController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Uploads an image file
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <returns>The URL of the uploaded image</returns>
    /// <response code="200">Returns the URL of the uploaded image</response>
    /// <response code="400">If the file is invalid or too large</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("upload")]
    [Authorize]
    [ProducesResponseType(typeof(ImageUploadResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [RequestSizeLimit(10485760)] // 10MB
    public async Task<ActionResult<ImageUploadResponse>> Upload(IFormFile file)
    {
        // Basic API-layer validation
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        // Create domain request model
        var uploadRequest = new ImageUploadRequest
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            FileStream = file.OpenReadStream()
        };

        // Delegate to domain layer (validation and storage handled there)
        var result = await _domainFacade.UploadImageAsync(uploadRequest);

        return Ok(new ImageUploadResponse { Url = result.Url });
    }

    /// <summary>
    /// Retrieves an image by key from blob storage
    /// </summary>
    /// <param name="key">The image key (filename)</param>
    /// <returns>The image file</returns>
    /// <response code="200">Returns the image file</response>
    /// <response code="404">If the image is not found</response>
    [HttpGet("/api/v1/images/{key}")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetImage(string key)
    {
        var result = await _domainFacade.GetImageAsync(key);
        
        if (result == null)
        {
            return NotFound();
        }

        return File(result.Stream, result.ContentType);
    }
}

/// <summary>
/// Response model for image upload
/// </summary>
public class ImageUploadResponse
{
    /// <summary>
    /// The URL of the uploaded image
    /// </summary>
    public string Url { get; set; } = string.Empty;
}

