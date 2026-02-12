using System.Diagnostics.CodeAnalysis;

namespace Orchestrator.Domain;

[ExcludeFromCodeCoverage]
public abstract class BaseException : Exception
{
    public abstract string Reason { get; }

    protected BaseException()
    {
    }

    protected BaseException(string message) : base(message)
    {
    }

    protected BaseException(string message, Exception inner) : base(message, inner)
    {
    }
}
