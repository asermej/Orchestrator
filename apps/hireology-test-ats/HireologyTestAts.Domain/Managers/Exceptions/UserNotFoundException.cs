using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class UserNotFoundException : NotFoundBaseException
{
    public override string Reason => "User not found";

    public UserNotFoundException() : base("User not found")
    {
    }

    public UserNotFoundException(string message) : base(message)
    {
    }
}
