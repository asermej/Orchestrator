using System.Text.Json.Serialization;

namespace HireologyTestAts.Domain;

/// <summary>
/// Request model for syncing an applicant to the Orchestrator API
/// </summary>
internal class OrchestratorSyncApplicantRequest
{
    [JsonPropertyName("externalApplicantId")]
    public string ExternalApplicantId { get; set; } = string.Empty;

    [JsonPropertyName("externalJobId")]
    public string ExternalJobId { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("organizationId")]
    public Guid? OrganizationId { get; set; }
}
