namespace DeviceSystemBackend.Messages;


public interface IUserMessage;

public static class UserMessages
{
    public record RegisterUser(string Username, string Password) : IUserMessage;
    public record UserRegistered : IUserMessage;
    public record Login(string Username, string Password) : IUserMessage;
    public record LoginCompleted : IUserMessage;
    public record DeleteUser(string Username) : IUserMessage;
    public record UserDeleted : IUserMessage;
}