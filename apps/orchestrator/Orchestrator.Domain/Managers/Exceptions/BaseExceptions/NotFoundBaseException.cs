using System.Diagnostics.CodeAnalysis;

namespace Orchestrator.Domain;

[ExcludeFromCodeCoverage]
public abstract class NotFoundBaseException : BusinessBaseException
{
    protected NotFoundBaseException()
    {
    }

    protected NotFoundBaseException(string message) : base(message)
    {
    }

    protected NotFoundBaseException(string message, Exception inner) : base(message, inner)
    {
    }
}