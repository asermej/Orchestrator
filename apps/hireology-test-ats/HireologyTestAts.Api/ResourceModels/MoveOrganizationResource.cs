namespace HireologyTestAts.Api.ResourceModels;

public class MoveOrganizationResource
{
    /// <summary>
    /// The new parent organization ID. Set to null to move to root level.
    /// </summary>
    public Guid? NewParentOrganizationId { get; set; }
}
