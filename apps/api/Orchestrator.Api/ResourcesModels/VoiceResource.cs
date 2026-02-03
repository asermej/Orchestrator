namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// A voice (prebuilt) for TTS.
/// </summary>
public class VoiceResource
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PreviewText { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public string VoiceType { get; set; } = "prebuilt";
}

/// <summary>
/// Response for GET /api/v1/Voice/elevenlabs.
/// </summary>
public class AvailableVoicesResponse
{
    public List<VoiceResource> CuratedPrebuiltVoices { get; set; } = new();
    public List<VoiceResource> UserVoices { get; set; } = new();
}

/// <summary>
/// Request for POST /api/v1/agents/{agentId}/voice/select.
/// </summary>
public class SelectAgentVoiceRequest
{
    public string VoiceProvider { get; set; } = "elevenlabs";
    public string VoiceType { get; set; } = "prebuilt";
    public string VoiceId { get; set; } = string.Empty;
    public string? VoiceName { get; set; }
}

/// <summary>
/// Request for POST /api/v1/Voice/preview.
/// </summary>
public class PreviewVoiceRequest
{
    public string VoiceId { get; set; } = string.Empty;
    public string Text { get; set; } = "Hey — I'm your Surrova agent voice.";
}

/// <summary>
/// Request for POST /api/v1/Agent/{agentId}/voice/test.
/// </summary>
public class TestAgentVoiceRequest
{
    public string Text { get; set; } = "Hey — I'm your Surrova agent voice.";
}
