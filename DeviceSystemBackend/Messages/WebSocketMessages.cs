using System.Net.WebSockets;

namespace DeviceSystemBackend.Messages;


public interface IWebSocketMessage;

public static class WebSocketMessages
{
    public record BroadcastDataToClients(object Data) : IWebSocketMessage;
    public record StartWebSocketConnection(WebSocket Socket) : IWebSocketMessage;
    public record SendData(object Data) : IWebSocketMessage;
    public record SentDataToClient : IWebSocketMessage;
    public record CheckSocketState : IWebSocketMessage;
    public record SocketClosed : IWebSocketMessage;
    public record SocketError(string Reason) : IWebSocketMessage;
}