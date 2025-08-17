using DeviceSystemBackend.EndpointHandlers;

namespace DeviceSystemBackend.EndpointMappers;

public static class UserEndpointsMapper
{
    private const string UserTag = "User Management API";
    private const string BasePath = "/api/v1/user";
    
    public static void MapUserEndpoints(this WebApplication app)
    {
        var userGroup = app.MapGroup(BasePath)
            .WithTags(UserTag)
            .WithOpenApi();
        
        userGroup.MapPost("/reg", UserEndpointsHandler.HandleUserRegistration)
            .WithName("RegisterUser")
            .WithDescription("Creates a new user account in the system")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        
        userGroup.MapPost("/auth", UserEndpointsHandler.HandleUserAuthorization)
            .WithName("AuthenticateUser")
            .WithDescription("Authenticates user credentials in the system")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        
        userGroup.MapDelete("/{userName}", UserEndpointsHandler.HandleUserDeletion)
            .WithName("DeleteUser")
            .WithDescription("Permanently removes a user account from the system")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}