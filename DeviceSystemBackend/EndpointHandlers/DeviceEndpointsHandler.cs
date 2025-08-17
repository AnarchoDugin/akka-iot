using Akka.Actor;
using DeviceSystemBackend.Main;
using DeviceSystemBackend.Messages;
using Microsoft.AspNetCore.Mvc;
using Exception = System.Exception;


namespace DeviceSystemBackend.EndpointHandlers;

public static class DeviceEndpointsHandler
{
    private const string UnexpectedError = "Unexpected server error";
    
    public static async Task<IResult> HandleDeviceRegistration(
        [FromBody] DataObjects.DeviceRegistrationRequest request,
        [FromServices] IActorRegistry actorRegistry) 
    {
        try
        {
            var deviceRegistrationRequest = await actorRegistry.DatabasePool.Ask<object>(new DeviceMessages.RegisterDevice(
                Name: request.Name,
                SerialNumber: request.SerialNumber,
                Location: request.Location)
            );

            return deviceRegistrationRequest switch
            {
                DeviceMessages.DeviceRegistered => CreateSuccessResponse(
                    statusCode: 201,
                    message: "Device created successfully",
                    data: new { name = request.Name, serialNumber = request.SerialNumber, location = request.Location, status = "offline" }
                ),

                ErrorMessages.DeviceAlreadyExists => CreateErrorResponse(
                    statusCode: 400,
                    message: "Device already exists"),
                
                _ => CreateErrorResponse(
                    statusCode: 500,
                    message: UnexpectedError)
            };

        }
        catch (Exception ex)
        {
            return CreateErrorResponse(500, UnexpectedError);
        }
    }

    public static async Task<IResult> HandleDeviceUnregistration(
        [FromRoute] string deviceName,
        [FromServices] IActorRegistry actorRegistry)
    {
        try
        {
            var deviceUnregisterRequest = await actorRegistry.DatabasePool.Ask<object>(new DeviceMessages.UnregisterDevice(
                Name: deviceName)
            );

            return deviceUnregisterRequest switch
            {
                DeviceMessages.DeviceUnregistered => CreateSuccessResponse(
                    statusCode: 200,
                    message: "Device unregistered successfully",
                    data: new { name = deviceName }),

                ErrorMessages.DeviceDoesNotExist => CreateErrorResponse(
                    statusCode: 400, 
                    message: "Device does not exist"),

                _ => CreateErrorResponse(
                    statusCode: 500,
                    UnexpectedError)
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(500, UnexpectedError);
        }
    }

    public static async Task<IResult> HandleDeviceStartup(
        [FromRoute] string deviceName,
        [FromServices] IActorRegistry actorRegistry)
    {
        try
        {
            var requestDeviceCheck = await actorRegistry.DatabasePool.Ask<object>(new DeviceMessages.CheckDeviceExists(
                DeviceName: deviceName));

            if (requestDeviceCheck is not DeviceMessages.DeviceChecked)
            {
                return CreateErrorResponse(400, "Can't start nonexistent device");
            }
            
            var requestDeviceStartup = await actorRegistry.DeviceManager.Ask<object>(new DeviceMessages.StartDevice(
                Name: deviceName,
                WebSocketManagerRef: actorRegistry.WebSocketManager)
            );

            return requestDeviceStartup switch
            {
                DeviceMessages.DeviceStarted => CreateSuccessResponse(
                    statusCode: 200,
                    message: "Device started successfully",
                    data: new { name = deviceName }),

                ErrorMessages.DeviceAlreadyOnline => CreateErrorResponse(
                    statusCode: 400,
                    message: "Device already started"),

                _ => CreateErrorResponse(
                    statusCode: 500,
                    UnexpectedError)
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(500, UnexpectedError);
        }
    }

    public static async Task<IResult> HandleDeviceShutdown(
        [FromRoute] string deviceName,
        [FromServices] IActorRegistry actorRegistry)
    {
        try
        {
            var requestDeviceCheck = await actorRegistry.DatabasePool.Ask<object>(new DeviceMessages.CheckDeviceExists(
                DeviceName: deviceName));

            if (requestDeviceCheck is not DeviceMessages.DeviceChecked)
            {
                return CreateErrorResponse(400, "Can't stop nonexistent device");
            }
            
            var requestStopDevice = await actorRegistry.DeviceManager.Ask<object>(new DeviceMessages.StopDevice(
                Name: deviceName)
            );

            return requestStopDevice switch
            {
                DeviceMessages.DeviceStopped => CreateSuccessResponse(
                    statusCode: 200,
                    message: "Device stopped successfully",
                    data: new { name = deviceName }),

                ErrorMessages.DeviceAlreadyOffline => CreateErrorResponse(
                    statusCode: 400,
                    message: "Device already offline"),

                _ => CreateErrorResponse(
                    statusCode: 500,
                    UnexpectedError)
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(500, UnexpectedError);
        }
    }

    public static async Task<IResult> HandleDeviceListRequest([FromServices] IActorRegistry actorRegistry)
    {
        try
        {
            var requestDeviceList = await actorRegistry.DatabasePool.Ask<object>(new DeviceMessages.RequestDeviceList());
            return requestDeviceList switch
            {
                DeviceMessages.RespondDeviceList deviceList => CreateSuccessResponse(
                    statusCode: 200,
                    message: "Device list returned",
                    data: deviceList.DeviceList),

                _ => CreateErrorResponse(
                    statusCode: 500,
                    UnexpectedError)
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(500, UnexpectedError);
        }
    }
    
    private static IResult CreateSuccessResponse(int statusCode, string message, object data)
    {
        return Results.Json(new
        {
            success = true,
            status = statusCode,
            message,
            data
        }, statusCode: statusCode);
    }

    private static IResult CreateErrorResponse(int statusCode, string message)
    {
        return Results.Json(new
        {
            success = false,
            status = statusCode,
            message
        }, statusCode: statusCode);
    }
}