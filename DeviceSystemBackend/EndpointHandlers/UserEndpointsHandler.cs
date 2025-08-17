using Akka.Actor;
using DeviceSystemBackend.Main;
using DeviceSystemBackend.Messages;
using Microsoft.AspNetCore.Mvc;
using Exception = System.Exception;

namespace DeviceSystemBackend.EndpointHandlers;

public static class UserEndpointsHandler
{
    private const string UnexpectedError = "Unexpected server error";
    
    public static async Task<IResult> HandleUserRegistration(
        [FromBody] DataObjects.UserRegistrationRequest request,
        [FromServices] IActorRegistry actorRegistry) 
    {
        try
        {
            var registerUserRequest = await actorRegistry.DatabasePool.Ask<object>(
                new UserMessages.RegisterUser(
                    request.Username,
                    request.Password
                    ));

            return registerUserRequest switch
            {
                UserMessages.UserRegistered => CreateSuccessResponse(
                    statusCode: 201,
                    message: "User created successfully",
                    data: new { name = request.Username }),
                    
                ErrorMessages.UserAlreadyExists => CreateErrorResponse(
                    statusCode: 400,
                    message: "User already exists"),
                    
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

    public static async Task<IResult> HandleUserAuthorization(
        [FromBody] DataObjects.UserLoginRequest request,
        [FromServices] IActorRegistry actorRegistry)
    {
        try
        {
            var loginUserRequest = await actorRegistry.DatabasePool.Ask<object>(
                new UserMessages.Login(request.Username, request.Password));

            return loginUserRequest switch
            {
                UserMessages.LoginCompleted => CreateSuccessResponse(
                    statusCode: 200,
                    message: "Login successful",
                    data: new { name = request.Username }),
                    
                ErrorMessages.LoginFailed => CreateErrorResponse(
                    statusCode: 400,
                    message: "Login failed"),
                    
                ErrorMessages.UserDoesNotExist => CreateErrorResponse(
                    statusCode: 400,
                    message: "User does not exist"),
                    
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

    public static async Task<IResult> HandleUserDeletion(
        [FromRoute] string userName,
        [FromServices] IActorRegistry actorRegistry)
    {
        try
        {
            var userDeleteRequest = await actorRegistry.DatabasePool.Ask<object>(
                new UserMessages.DeleteUser(userName));

            return userDeleteRequest switch
            {
                UserMessages.UserDeleted => CreateSuccessResponse(
                    statusCode: 200,
                    message: "User deleted successfully",
                    data: new { name = userName }),

                ErrorMessages.UserDoesNotExist => CreateErrorResponse(
                    statusCode: 400,
                    message: "User does not exist"),

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

    private static IResult CreateSuccessResponse(int statusCode, string message, object data)
    {
        return Results.Json(new
        {
            success = true,
            status = statusCode,
            message,
            user = data
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