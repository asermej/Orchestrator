namespace HireologyTestAts.Domain;

public class OrganizationAccessEntry
{
    public Guid OrganizationId { get; set; }
    public bool IncludeChildren { get; set; }
}
