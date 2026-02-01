using System;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade : IDisposable
{
    private bool _disposed;
    private readonly ServiceLocatorBase _serviceLocator;
    private UserManager? _userManager;
    private UserManager UserManager => _userManager ??= new UserManager(_serviceLocator);
    private PersonaManager? _personaManager;
    private PersonaManager PersonaManager => _personaManager ??= new PersonaManager(_serviceLocator);
    private ChatManager? _chatManager;
    private ChatManager ChatManager => _chatManager ??= new ChatManager(_serviceLocator);
    private MessageManager? _messageManager;
    private MessageManager MessageManager => _messageManager ??= new MessageManager(_serviceLocator);
    private ImageManager? _imageManager;
    private ImageManager ImageManager => _imageManager ??= new ImageManager(_serviceLocator);
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);
    private TopicManager? _topicManager;
    private TopicManager TopicManager => _topicManager ??= new TopicManager(_serviceLocator);
    private CategoryManager? _categoryManager;
    private CategoryManager CategoryManager => _categoryManager ??= new CategoryManager(_serviceLocator);
    private TagManager? _tagManager;
    private TagManager TagManager => _tagManager ??= new TagManager(_serviceLocator);
    private AudioCacheManager? _audioCacheManager;
    private AudioCacheManager AudioCacheManager => _audioCacheManager ??= new AudioCacheManager(_serviceLocator);


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
            _personaManager?.Dispose();
            _chatManager?.Dispose();
            _messageManager?.Dispose();
            _imageManager?.Dispose();
            _gatewayFacade?.Dispose();
            _topicManager?.Dispose();
            _categoryManager?.Dispose();
            _tagManager?.Dispose();
            _conversationManager?.Dispose();
            _audioCacheManager?.Dispose();
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
