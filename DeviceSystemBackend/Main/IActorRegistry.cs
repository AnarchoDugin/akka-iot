using Akka.Actor;

namespace DeviceSystemBackend.Main;

public interface IActorRegistry
{
    IActorRef DatabasePool { get; }
    IActorRef DeviceManager { get; }
    IActorRef WebSocketManager { get; }

    void RegisterActors(IActorRef databasePool, IActorRef deviceManager, IActorRef webSocketManager);
}

public class DefaultActorRegistry : IActorRegistry
{
    private IActorRef? _databasePool;
    private IActorRef? _deviceManager;
    private IActorRef? _webSocketManager;
    
    public void RegisterActors(IActorRef databasePool, IActorRef deviceManager, IActorRef webSocketManager)
    {
        _databasePool = databasePool ?? throw new ArgumentNullException(nameof(databasePool));
        _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
    }

    public IActorRef DatabasePool =>
        _databasePool ?? throw new NullReferenceException(nameof(_databasePool));
    public IActorRef DeviceManager =>
        _deviceManager ?? throw new NullReferenceException(nameof(_deviceManager));
    public IActorRef WebSocketManager =>
        _webSocketManager ?? throw new NullReferenceException(nameof(_webSocketManager));
}