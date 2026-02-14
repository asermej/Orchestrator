namespace HireologyTestAts.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Auth0Sub { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public bool IsSuperadmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
