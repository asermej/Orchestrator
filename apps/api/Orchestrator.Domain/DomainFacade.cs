using System;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade : IDisposable
{
    private bool _disposed;
    private readonly ServiceLocatorBase _serviceLocator;
    
    // Existing managers
    private UserManager? _userManager;
    private UserManager UserManager => _userManager ??= new UserManager(_serviceLocator);
    private AgentManager? _agentManager;
    private AgentManager AgentManager => _agentManager ??= new AgentManager(_serviceLocator);
    private ImageManager? _imageManager;
    private ImageManager ImageManager => _imageManager ??= new ImageManager(_serviceLocator);
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);
    private AudioCacheManager? _audioCacheManager;
    private AudioCacheManager AudioCacheManager => _audioCacheManager ??= new AudioCacheManager(_serviceLocator);
    
    // New ATS Platform managers
    private OrganizationManager? _organizationManager;
    private OrganizationManager OrganizationManager => _organizationManager ??= new OrganizationManager(_serviceLocator);
    private ApplicantManager? _applicantManager;
    private ApplicantManager ApplicantManager => _applicantManager ??= new ApplicantManager(_serviceLocator);
    private JobManager? _jobManager;
    private JobManager JobManager => _jobManager ??= new JobManager(_serviceLocator);
    private InterviewManager? _interviewManager;
    private InterviewManager InterviewManager => _interviewManager ??= new InterviewManager(_serviceLocator);
    private InterviewConfigurationManager? _interviewConfigurationManager;
    private InterviewConfigurationManager InterviewConfigurationManager => _interviewConfigurationManager ??= new InterviewConfigurationManager(_serviceLocator);
    private WebhookManager? _webhookManager;
    private DataFacade? _webhookDataFacade;
    private WebhookManager WebhookManager
    {
        get
        {
            if (_webhookManager == null)
            {
                _webhookDataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());
                _webhookManager = new WebhookManager(_webhookDataFacade);
            }
            return _webhookManager;
        }
    }

    public DomainFacade() : this(new ServiceLocator()) { }

    internal DomainFacade(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    private void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _userManager?.Dispose();
            _agentManager?.Dispose();
            _imageManager?.Dispose();
            _gatewayFacade?.Dispose();
            _audioCacheManager?.Dispose();
            _organizationManager?.Dispose();
            _applicantManager?.Dispose();
            _jobManager?.Dispose();
            _interviewManager?.Dispose();
            _interviewConfigurationManager?.Dispose();
            _webhookManager?.Dispose();
            _conversationManager?.Dispose();
            _voiceManager?.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
