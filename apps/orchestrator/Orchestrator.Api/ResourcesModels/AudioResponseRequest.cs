using System;
using System.ComponentModel.DataAnnotations;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Request model for streaming audio response endpoint.
/// The message is TEXT (already transcribed by browser via Web Speech API).
/// Stateless: no chat or message persistence.
/// </summary>
public class AudioResponseRequest
{
    /// <summary>
    /// The agent ID to get voice settings from
    /// </summary>
    [Required]
    public Guid AgentId { get; set; }

    /// <summary>
    /// The user's text message (already transcribed from speech in the browser)
    /// </summary>
    [Required]
    [StringLength(10000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}
