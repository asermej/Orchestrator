using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public abstract class TechnicalBaseException : BaseException
{
    protected TechnicalBaseException()
    {
    }

    protected TechnicalBaseException(string message) : base(message)
    {
    }

    protected TechnicalBaseException(string message, Exception inner) : base(message, inner)
    {
    }
}
