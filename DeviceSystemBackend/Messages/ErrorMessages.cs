namespace DeviceSystemBackend.Messages;


public interface IErrorMessage;

public static class ErrorMessages
{
    public record DeviceAlreadyExists : IErrorMessage;
    public record DeviceDoesNotExist : IErrorMessage;
    public record UserAlreadyExists : IErrorMessage;
    public record UserDoesNotExist : IErrorMessage;
    public record LoginFailed : IErrorMessage;
    public record DatabaseError : IErrorMessage;
    public record DeviceAlreadyOnline : IErrorMessage;
    public record DeviceAlreadyOffline : IErrorMessage;
    public record UnexpectedError : IErrorMessage;
}