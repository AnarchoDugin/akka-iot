using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using DeviceSystemBackend.Messages;


namespace DeviceSystemBackend.Actors.Devices
{
    public class DeviceManagerActor : UntypedActor
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();
        
        private readonly Dictionary<IActorRef, string> _actorToDeviceName = new();
        private readonly Dictionary<string, IActorRef> _deviceNameToActor = new();

        protected override void PreStart() => _logger.Info("DeviceManager started");
        protected override void PostStop() => _logger.Info("DeviceManager stopped");

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case DeviceMessages.StartDevice start:
                    HandleStartDevice(start);
                    break;
                
                case DeviceMessages.StopDevice stop:
                    HandleStopDevice(stop);
                    break;
                
                case DeviceMessages.RequestActorRefList:
                    HandleReplyActorRefList();
                    break;
                
                case DeviceMessages.RequestDeviceNameList:
                    HandleReplyDeviceNameList();
                    break;
                
                case Terminated terminated:
                    HandleTerminatedDevice(terminated);
                    break;
                
                default:
                    Unhandled(message);
                    break;
            }
        }

        private void HandleStartDevice(DeviceMessages.StartDevice message)
        {
            if (_deviceNameToActor.TryGetValue(message.Name, out var existingActor))
            {
                _logger.Warning($"Attempt to start already running device: {message.Name}");
                Sender.Tell(new ErrorMessages.DeviceAlreadyOnline());
                return;
            }

            var deviceActor = CreateDeviceActor(message);
            TrackDeviceActor(deviceActor, message.Name);
            
            deviceActor.Forward(message);
            _logger.Info($"Attempt to start a new device: {message.Name}");
        }

        private IActorRef CreateDeviceActor(DeviceMessages.StartDevice message)
        {
            return Context.ActorOf(
                DeviceNodeActor.Props(
                    name: message.Name,
                    webSocketManagerActor: message.WebSocketManagerRef),
                name: $"device-{message.Name}");
        }

        private void TrackDeviceActor(IActorRef actorRef, string deviceName)
        {
            Context.Watch(actorRef);
            _actorToDeviceName[actorRef] = deviceName;
            _deviceNameToActor[deviceName] = actorRef;
        }

        private void UntrackDeviceActor(IActorRef actorRef, string deviceName)
        {
            Context.Unwatch(actorRef);
            _actorToDeviceName.Remove(actorRef);
            _deviceNameToActor.Remove(deviceName);
        }

        private void HandleStopDevice(DeviceMessages.StopDevice message)
        {
            if (!_deviceNameToActor.TryGetValue(message.Name, out var deviceActor))
            {
                _logger.Warning($"Attempt to stop non-existent device: {message.Name}");
                Sender.Tell(new ErrorMessages.DeviceAlreadyOffline());
                return;
            }

            deviceActor.Forward(message);
            _logger.Info($"Attempt to stop a device: {message.Name}");
        }

        private void HandleTerminatedDevice(Terminated message)
        {
            if (!_actorToDeviceName.TryGetValue(message.ActorRef, out var deviceName))
            {
                _logger.Warning($"Received Terminated for unknown actor: {message.ActorRef}");
                return;
            }
            
            UntrackDeviceActor(message.ActorRef, deviceName);
            _logger.Info($"Device terminated and cleaned up: {deviceName}");
        }

        private void HandleReplyActorRefList()
        {
            var replyList = _deviceNameToActor.Values.ToImmutableList();
            Sender.Tell(new DeviceMessages.ReplyActorRefList(replyList));
        }

        private void HandleReplyDeviceNameList()
        {
            var replyList = _deviceNameToActor.Keys.ToImmutableList();
            Sender.Tell(new DeviceMessages.ReplyDeviceNameList(replyList));
        }

        public static Props Props() => Akka.Actor.Props.Create(() => new DeviceManagerActor());
    }
}