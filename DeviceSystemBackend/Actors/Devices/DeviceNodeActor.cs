using Akka.Actor;
using Akka.Event;
using DeviceSystemBackend.Messages;


namespace DeviceSystemBackend.Actors.Devices
{
    public class DeviceNodeActor : UntypedActor
    {
        private const double DefaultDataIntervalSeconds = 2.0;
        
        private readonly string _deviceName;
        private readonly IActorRef _webSocketManagerActor;
        private readonly ILoggingAdapter _logger;
        private readonly Random _randomGenerator = new();
        private readonly ICancelable _dataSendingSchedule;
        private readonly DateTime _startTimeUtc;

        public DeviceNodeActor(string deviceName, IActorRef webSocketManagerActor)
        {
            _deviceName = deviceName ?? throw new ArgumentNullException(nameof(deviceName));
            _webSocketManagerActor = webSocketManagerActor ?? throw new ArgumentNullException(nameof(webSocketManagerActor));
            
            _logger = Context.GetLogger();
            _startTimeUtc = DateTime.UtcNow;
            _dataSendingSchedule = ScheduleDataSending();
        }

        protected override void PreStart() => _logger.Info($"Device [{_deviceName}] started");
        
        protected override void PostStop()
        {
            _logger.Info($"Device [{_deviceName}] stopped");
            _dataSendingSchedule.Cancel();
        }

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
                
                case DeviceMessages.SendCurrentData send:
                    HandleSendCurrentData(send);
                    break;
                
                default:
                    Unhandled(message);
                    break;
            }
        }

        private void HandleStartDevice(DeviceMessages.StartDevice msg)
        {
            Sender.Tell(new DeviceMessages.DeviceStarted());
            _logger.Info($"Device [{_deviceName}] received start command");
        }

        private void HandleStopDevice(DeviceMessages.StopDevice msg)
        {
            Sender.Tell(new DeviceMessages.DeviceStopped());
            _logger.Info($"Device [{_deviceName}] received stop command");
            Context.Stop(Self);
        }

        private void HandleSendCurrentData(DeviceMessages.SendCurrentData msg)
        {
            var deviceData = GenerateDeviceData();
            _webSocketManagerActor.Tell(new WebSocketMessages.BroadcastDataToClients(deviceData));
        }

        private object GenerateDeviceData()
        {
            var temperature = GenerateRandomValue(0, 39);
            var humidity = GenerateRandomValue(0, 99);
            
            return new
            {
                deviceName = _deviceName,
                temperature,
                humidity,
                deviceUpTime = DateTime.UtcNow - _startTimeUtc
            };
        }

        private double GenerateRandomValue(int min, int max)
        {
            return _randomGenerator.Next(min, max) + _randomGenerator.NextDouble();
        }

        private ICancelable ScheduleDataSending()
        {
            return Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                initialDelay: TimeSpan.Zero,
                interval: TimeSpan.FromSeconds(DefaultDataIntervalSeconds),
                receiver: Self,
                message: new Messages.DeviceMessages.SendCurrentData(),
                sender: Self);
        }

        public static Props Props(string name, IActorRef webSocketManagerActor) 
            => Akka.Actor.Props.Create(() => new DeviceNodeActor(name, webSocketManagerActor));
    }
}