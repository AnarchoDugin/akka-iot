using Akka.Actor;
using Akka.Routing;
using DeviceSystemBackend.Actors.Database;
using DeviceSystemBackend.Actors.Devices;
using DeviceSystemBackend.Actors.WebSocket;
using DeviceSystemBackend.EndpointMappers;
using Microsoft.OpenApi.Models;
using Npgsql;


namespace DeviceSystemBackend.Main;

public static class DeviceSystemWebApi
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var postgresConnectionString = builder.Configuration.GetConnectionString("PostgreSQL")
                                       ?? throw new ConfigurationException("PostgreSQL connection string is missing");
        var postgresActorPoolSize = builder.Configuration.GetValue<int?>("Akka:DatabaseActorPoolSize") 
                                    ?? throw new ConfigurationException("Database actor pool size is missing");

        ConfigureServices(builder.Services);
        
        var app = builder.Build();
        
        ConfigureMiddleware(app);
        ConfigureActorSystem(app, postgresConnectionString, postgresActorPoolSize);
        ConfigureApiEndpoints(app);
        
        await app.RunAsync();
    }

    private static void ConfigureApiEndpoints(WebApplication app)
    {
        app.MapWebSocketEndpoint();
        app.MapUserEndpoints();
        app.MapDeviceEndpoints();
    }

    private static void ConfigureActorSystem(
        WebApplication app,
        string postgresConnectionString,
        int postgresActorPoolSize)
    {
        var system = ActorSystem.Create("IoT-System");
        var postgreSqlDataSource = NpgsqlDataSource.Create(postgresConnectionString);
        
        var databaseActorPool = system.ActorOf(
            DatabaseQueryActor.Props(postgreSqlDataSource)
                .WithRouter(new RoundRobinPool(postgresActorPoolSize)), "database-pool");
        
        var deviceManager = system.ActorOf(
            DeviceManagerActor.Props(), "device-manager");
        
        var webSocketManager = system.ActorOf(
            WebSocketManagerActor.Props(), "websocket-manager");
        
        app.Services.GetRequiredService<IActorRegistry>()
            .RegisterActors(databaseActorPool, deviceManager, webSocketManager);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IActorRegistry>(new DefaultActorRegistry());
        
        services.AddEndpointsApiExplorer(); 
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Device System API",
                Version = "v1",
                Description = "CRUD API for managing IoT devices",
                Contact = new OpenApiContact
                {
                    Name = "zakuwarrior",
                    Email = "abramov.ivic@gmail.com"
                }
            });
        });
        
        services.AddCors(options => {
            options.AddPolicy("AllowFrontendServer", policy => {
                policy.WithOrigins("http://localhost:5500", "http://127.0.0.1:5500")
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseCors("AllowFrontendServer");
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseWebSockets();
    }
}

public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) {}
}