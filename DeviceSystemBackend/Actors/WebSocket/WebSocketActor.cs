using System.Net.WebSockets;
using System.Text;
using Akka.Actor;
using Akka.Event;
using DeviceSystemBackend.Messages;
using Newtonsoft.Json;


namespace DeviceSystemBackend.Actors.WebSocket
{
    public class WebSocketActor : ReceiveActor
    {
        private readonly Guid _clientId;
        private readonly System.Net.WebSockets.WebSocket _clientSocket;
        private readonly ILoggingAdapter _logger = Context.GetLogger();
        private readonly ICancelable _socketCheckTimer;
        private readonly IActorRef _self;

        protected override void PreStart()
        {
            _logger.Info($"WebSocket connection established for client: {_clientId}");
            StartReceiveLoop();
        }

        protected override void PostStop()
        {
            _socketCheckTimer.Cancel();
            
            try
            {
                if (_clientSocket.State == WebSocketState.Open)
                {
                    _clientSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "WebSocket actor stopped",
                        CancellationToken.None).Wait(1000);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while closing WebSocket for client: {_clientId}");
            }
            finally
            {
                _clientSocket.Dispose();
                _logger.Info($"WebSocket connection closed for client: {_clientId}");
            }
        }

        public WebSocketActor(Guid clientId, System.Net.WebSockets.WebSocket clientSocket)
        {
            _clientId = clientId;
            _clientSocket = clientSocket ?? throw new ArgumentNullException(nameof(clientSocket));

            Receive<WebSocketMessages.SendData>(HandleSocketTransmission);
            
            Receive<WebSocketMessages.SentDataToClient>(_ => _logger.Info($"Data sent to client: {_clientId}"));
            
            Receive<WebSocketMessages.CheckSocketState>(_ => CheckSocketConnection());
            
            Receive<WebSocketMessages.SocketClosed>(_ => Context.Stop(Self));
            
            Receive<WebSocketMessages.SocketError>(e => {
                _logger.Warning($"WebSocket error for client {_clientId}: {e.Reason}");
                Context.Stop(Self);
            });
            
            _self = Self; 
            _socketCheckTimer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5),
                Self,
                new WebSocketMessages.CheckSocketState(),
                Self);
        }

        private void StartReceiveLoop()
        {
            ReceiveLoopAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _self.Tell(new WebSocketMessages.SocketError(task.Exception?.Message ?? "Unknown error"));
                }
            });
        }

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[1024];
            try
            {
                while (_clientSocket.State == WebSocketState.Open)
                {
                    var result = await _clientSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), 
                       CancellationToken.None);

                    if (result.MessageType != WebSocketMessageType.Close) continue;
                    
                    _self.Tell(new WebSocketMessages.SocketClosed());
                    break;
                }
            }
            catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
            {
                _self.Tell(new WebSocketMessages.SocketError(ex.Message));
            }
        }

        private void CheckSocketConnection()
        {
            if (_clientSocket.State != WebSocketState.Open)
            {
                _self.Tell(new WebSocketMessages.SocketClosed());
            }
        }

        private void HandleSocketTransmission(WebSocketMessages.SendData msg)
        {
            SendDataToClientAsync(msg.Data)
                .ContinueWith(task =>
                    task.IsFaulted ? new WebSocketMessages.SocketError(task.Exception?.Message ?? "Send failed") : task.Result)
                .PipeTo(_self);
        }

        private async Task<object> SendDataToClientAsync(object data)
        {
            if (_clientSocket.State != WebSocketState.Open)
                return new WebSocketMessages.SocketClosed();

            try
            {
                var json = JsonConvert.SerializeObject(data);
                var bytes = Encoding.UTF8.GetBytes(json);
                
                await _clientSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
                
                return new WebSocketMessages.SentDataToClient();
            }
            catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException)
            {
                return new WebSocketMessages.SocketError(ex.Message);
            }
        }
        
        public static Props Props(Guid clientId, System.Net.WebSockets.WebSocket clientSocket) =>
            Akka.Actor.Props.Create(() => new WebSocketActor(clientId, clientSocket));
    }
}