namespace HireologyTestAts.Api.ResourceModels;

public class GroupResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateGroupResource
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateGroupResource
{
    public string? Name { get; set; }
}
