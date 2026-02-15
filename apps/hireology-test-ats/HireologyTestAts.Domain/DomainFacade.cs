using System;

namespace HireologyTestAts.Domain;

public sealed partial class DomainFacade : IDisposable
{
    private bool _disposed;
    private readonly ServiceLocatorBase _serviceLocator;

    private GroupManager? _groupManager;
    private GroupManager GroupManager => _groupManager ??= new GroupManager(_serviceLocator);

    private OrganizationManager? _organizationManager;
    private OrganizationManager OrganizationManager => _organizationManager ??= new OrganizationManager(_serviceLocator);

    private JobManager? _jobManager;
    private JobManager JobManager => _jobManager ??= new JobManager(_serviceLocator);

    private UserManager? _userManager;
    private UserManager UserManager => _userManager ??= new UserManager(_serviceLocator);

    private ApplicantManager? _applicantManager;
    private ApplicantManager ApplicantManager => _applicantManager ??= new ApplicantManager(_serviceLocator);

    private InterviewRequestManager? _interviewRequestManager;
    private InterviewRequestManager InterviewRequestManager => _interviewRequestManager ??= new InterviewRequestManager(_serviceLocator);

    public DomainFacade() : this(new ServiceLocator()) { }

    internal DomainFacade(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    private void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _groupManager?.Dispose();
            _organizationManager?.Dispose();
            _jobManager?.Dispose();
            _userManager?.Dispose();
            _applicantManager?.Dispose();
            _interviewRequestManager?.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
