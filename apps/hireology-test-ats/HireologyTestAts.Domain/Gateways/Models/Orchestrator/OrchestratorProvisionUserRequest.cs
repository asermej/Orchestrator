using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Request model for provisioning a user in the Orchestrator API
/// </summary>
internal class OrchestratorProvisionUserRequest
{
    [JsonPropertyName("auth0Sub")]
    public string Auth0Sub { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}
