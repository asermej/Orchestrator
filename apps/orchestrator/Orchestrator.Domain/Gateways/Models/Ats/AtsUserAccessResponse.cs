using System.Text.Json.Serialization;

namespace Orchestrator.Domain;

/// <summary>
/// Response from the ATS /external/user-access endpoint
/// </summary>
internal class AtsUserAccessResponse
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("auth0Sub")]
    public string Auth0Sub { get; set; } = string.Empty;

    [JsonPropertyName("isSuperadmin")]
    public bool IsSuperadmin { get; set; }

    [JsonPropertyName("isGroupAdmin")]
    public bool IsGroupAdmin { get; set; }

    [JsonPropertyName("adminGroupIds")]
    public List<Guid> AdminGroupIds { get; set; } = new();

    [JsonPropertyName("accessibleGroups")]
    public List<AtsGroupInfoResponse> AccessibleGroups { get; set; } = new();

    [JsonPropertyName("accessibleOrganizations")]
    public List<AtsOrganizationInfoResponse> AccessibleOrganizations { get; set; } = new();
}

internal class AtsGroupInfoResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

internal class AtsOrganizationInfoResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("groupId")]
    public Guid GroupId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
