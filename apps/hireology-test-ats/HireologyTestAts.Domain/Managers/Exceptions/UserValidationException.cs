using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class UserValidationException : BusinessBaseException
{
    public override string Reason { get; }

    public UserValidationException(string reason) : base(reason)
    {
        Reason = reason;
    }
}
