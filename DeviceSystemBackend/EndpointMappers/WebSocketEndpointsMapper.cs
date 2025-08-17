using DeviceSystemBackend.EndpointHandlers;

namespace DeviceSystemBackend.EndpointMappers;

public static class WebSocketEndpointsMapper
{
    public static void MapWebSocketEndpoint(this WebApplication app)
    {
        app.Map("/ws", WebSocketEndpointsHandler.HandleWebSocketRequest)
            .WithName("WebSocketConnection")
            .WithDescription("Establish WebSocket connection to the server")
            .Produces(StatusCodes.Status101SwitchingProtocols)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}