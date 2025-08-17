using Akka.Actor;
using DeviceSystemBackend.Main;
using Microsoft.AspNetCore.Mvc;
using DeviceSystemBackend.Messages;


namespace DeviceSystemBackend.EndpointHandlers;

public static class WebSocketEndpointsHandler
{
    public static async Task HandleWebSocketRequest([FromServices] IActorRegistry actorRegistry, HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket connection expected");
            return;
        }
        
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var webSocketManager = actorRegistry.WebSocketManager;
        
        webSocketManager.Tell(new WebSocketMessages.StartWebSocketConnection(webSocket));
        await Task.Delay(Timeout.Infinite);
    }
}