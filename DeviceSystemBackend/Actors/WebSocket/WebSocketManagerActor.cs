using Akka.Actor;
using Akka.Event;
using DeviceSystemBackend.Messages;


namespace DeviceSystemBackend.Actors.WebSocket
{
    public class WebSocketManagerActor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();
        private readonly Dictionary<Guid, IActorRef> _clientActors = new();

        protected override void PreStart() => _logger.Info("WebSocketManager started");
        protected override void PostStop() => _logger.Info("WebSocketManager stopped");

        public WebSocketManagerActor()
        {
            Receive<WebSocketMessages.StartWebSocketConnection>(HandleNewConnection);
            Receive<WebSocketMessages.BroadcastDataToClients>(HandleBroadcast);
            Receive<Terminated>(HandleTerminated);
        }

        private void HandleNewConnection(WebSocketMessages.StartWebSocketConnection message)
        {
            var clientId = Guid.NewGuid();
            _logger.Info($"Attempt to establish a new connection for client: {clientId}");
            
            var webSocketActor = CreateWebSocketActor(message, clientId);
            TrackClientActor(webSocketActor, clientId);
        }

        private IActorRef CreateWebSocketActor(WebSocketMessages.StartWebSocketConnection message, Guid clientId)
        {
            return Context.ActorOf(
                WebSocketActor.Props(clientId, message.Socket),
                $"websocket-{clientId}");
        }

        private void TrackClientActor(IActorRef actorRef, Guid clientId)
        {
            Context.Watch(actorRef);
            _clientActors[clientId] = actorRef;
        }

        private void HandleBroadcast(WebSocketMessages.BroadcastDataToClients message)
        {
            foreach (var actor in _clientActors.Values)
            {
                actor.Tell(new WebSocketMessages.SendData(message.Data));
            }
            
            _logger.Debug($"Broadcast message to {_clientActors.Count} websocket clients");
        }

        private void HandleTerminated(Terminated message)
        {
            var clientId = FindClientIdByActor(message.ActorRef);
            if (clientId.HasValue)
            {
                RemoveClientActor(clientId.Value, message.ActorRef);
            }
        }

        private Guid? FindClientIdByActor(IActorRef actorRef)
        {
            foreach (var pair in _clientActors)
            {
                if (pair.Value.Equals(actorRef))
                {
                    return pair.Key;
                }
            }
            return null;
        }

        private void RemoveClientActor(Guid clientId, IActorRef actorRef)
        {
            Context.Unwatch(actorRef);
            _clientActors.Remove(clientId);
        }

        public static Props Props() => Akka.Actor.Props.Create(() => new WebSocketManagerActor());
    }
}