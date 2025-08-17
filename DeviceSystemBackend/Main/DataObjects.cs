namespace DeviceSystemBackend.Main;

public static class DataObjects
{
    public record DeviceRegistrationRequest(string Name, string SerialNumber, string Location);
    public record UserRegistrationRequest(string Username, string Password);
    public record UserLoginRequest(string Username, string Password);
}