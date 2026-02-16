using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Response model for refreshing an interview invite in the Orchestrator API
/// </summary>
internal class OrchestratorRefreshInviteResponse
{
    [JsonPropertyName("invite")]
    public OrchestratorRefreshInviteResponseInvite Invite { get; set; } = new();

    [JsonPropertyName("inviteUrl")]
    public string? InviteUrl { get; set; }
}

internal class OrchestratorRefreshInviteResponseInvite
{
    [JsonPropertyName("shortCode")]
    public string ShortCode { get; set; } = string.Empty;
}
