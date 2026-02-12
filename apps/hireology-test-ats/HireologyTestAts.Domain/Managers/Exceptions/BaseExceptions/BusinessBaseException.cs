using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public abstract class BusinessBaseException : BaseException
{
    protected BusinessBaseException()
    {
    }

    protected BusinessBaseException(string message) : base(message)
    {
    }

    protected BusinessBaseException(string message, Exception inner) : base(message, inner)
    {
    }
}
