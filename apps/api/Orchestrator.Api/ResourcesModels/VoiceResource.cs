namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// A voice (prebuilt or user-cloned) for TTS.
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
/// Request for POST /api/v1/personas/{personaId}/voice/select.
/// </summary>
public class SelectPersonaVoiceRequest
{
    public string VoiceProvider { get; set; } = "elevenlabs";
    public string VoiceType { get; set; } = "prebuilt";
    public string VoiceId { get; set; } = string.Empty;
    public string? VoiceName { get; set; }
}

/// <summary>
/// Request for POST /api/v1/Voice/clone (multipart: file + form fields).
/// </summary>
public class CloneVoiceRequest
{
    public Guid PersonaId { get; set; }
    public string VoiceName { get; set; } = string.Empty;
    public string? SampleBlobUrl { get; set; }
    public int SampleDurationSeconds { get; set; }
    public Guid ConsentRecordId { get; set; }
}

/// <summary>
/// Response for POST /api/v1/Voice/clone.
/// </summary>
public class CloneVoiceResponse
{
    public string VoiceId { get; set; } = string.Empty;
    public string VoiceName { get; set; } = string.Empty;
}

/// <summary>
/// Request for POST /api/v1/Voice/consent.
/// </summary>
public class RecordConsentRequest
{
    public Guid PersonaId { get; set; }
    public string? ConsentTextVersion { get; set; }
    public bool Attested { get; set; }
}

/// <summary>
/// Response for POST /api/v1/Voice/consent.
/// </summary>
public class RecordConsentResponse
{
    public Guid ConsentRecordId { get; set; }
}

/// <summary>
/// Request for POST /api/v1/Voice/preview.
/// </summary>
public class PreviewVoiceRequest
{
    public string VoiceId { get; set; } = string.Empty;
    public string Text { get; set; } = "Hey — I'm your Surrova persona voice.";
}

/// <summary>
/// Request for POST /api/v1/Persona/{personaId}/voice/test.
/// </summary>
public class TestPersonaVoiceRequest
{
    public string Text { get; set; } = "Hey — I'm your Surrova persona voice.";
}
