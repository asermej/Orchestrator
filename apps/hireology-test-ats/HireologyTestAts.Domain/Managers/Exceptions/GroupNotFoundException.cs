using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class GroupNotFoundException : NotFoundBaseException
{
    public override string Reason => "Group not found";

    public GroupNotFoundException() : base("Group not found")
    {
    }

    public GroupNotFoundException(string message) : base(message)
    {
    }
}
