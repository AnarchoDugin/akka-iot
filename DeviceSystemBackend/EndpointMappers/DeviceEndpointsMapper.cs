using DeviceSystemBackend.EndpointHandlers;

namespace DeviceSystemBackend.EndpointMappers;

public static class DeviceEndpointsMapper
{
    private const string DeviceTag = "Device Management API";
    private const string BasePath = "/api/v1/device";
    
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        var deviceGroup = app.MapGroup(BasePath)
            .WithTags(DeviceTag)
            .WithOpenApi();
        
        deviceGroup.MapPost("", DeviceEndpointsHandler.HandleDeviceRegistration)
            .WithName("RegisterDevice")
            .WithDescription("Register a new device in the system")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        
        deviceGroup.MapDelete("/{deviceName}", DeviceEndpointsHandler.HandleDeviceUnregistration)
            .WithName("UnregisterDevice")
            .WithDescription("Removes a device from the system")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        
        deviceGroup.MapPost("/{deviceName}/start", DeviceEndpointsHandler.HandleDeviceStartup)
            .WithName("StartDevice")
            .WithDescription("Initiates device startup sequence")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        
        deviceGroup.MapPost("/{deviceName}/stop", DeviceEndpointsHandler.HandleDeviceShutdown)
            .WithName("StopDevice")
            .WithDescription("Initiates device shutdown sequence")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        
        deviceGroup.MapGet("/list", DeviceEndpointsHandler.HandleDeviceListRequest)
            .WithName("GetDevices")
            .WithDescription("Retrieves list of all registered devices")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}