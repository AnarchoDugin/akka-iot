using Akka.Actor;

namespace DeviceSystemBackend.Messages;


public interface IDeviceMessage;

public static class DeviceMessages
{
    public record SendCurrentData : IDeviceMessage;
    public record RegisterDevice(string Name, string SerialNumber, string Location) : IDeviceMessage;
    public record DeviceRegistered : IDeviceMessage;
    public record UnregisterDevice(string Name) : IDeviceMessage;
    public record DeviceUnregistered : IDeviceMessage;
    public record StopDevice(string Name) : IDeviceMessage;
    public record DeviceStopped : IDeviceMessage;
    public record StartDevice(string Name, IActorRef WebSocketManagerRef) : IDeviceMessage;
    public record DeviceStarted : IDeviceMessage;
    public record RequestDeviceList : IDeviceMessage;
    public record RespondDeviceList(IList<object> DeviceList) : IDeviceMessage;
    public record CheckDeviceExists(string DeviceName) : IDeviceMessage;
    public record DeviceChecked : IDeviceMessage;
    public record RequestActorRefList : IDeviceMessage;
    public record ReplyActorRefList(IList<IActorRef> ActorRefs) : IDeviceMessage;
    public record RequestDeviceNameList : IDeviceMessage;
    public record ReplyDeviceNameList(IList<string> DeviceNames) : IDeviceMessage;
}