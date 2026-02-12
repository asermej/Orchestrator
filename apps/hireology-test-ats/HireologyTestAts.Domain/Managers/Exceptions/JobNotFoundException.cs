using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class JobNotFoundException : NotFoundBaseException
{
    public override string Reason => "Job not found";

    public JobNotFoundException() : base("Job not found")
    {
    }

    public JobNotFoundException(string message) : base(message)
    {
    }
}
