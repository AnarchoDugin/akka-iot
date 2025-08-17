using DeviceSystemBackend.Actors.Devices;
using DeviceSystemBackend.Actors.WebSocket;
using DeviceSystemBackend.Messages;
using FluentAssertions;

namespace DeviceSystemBackendTesting;

public class DeviceManagerActorTester : Akka.TestKit.Xunit2.TestKit
{
    [Fact]
    public void Device_Manager_Should_Not_Contain_Any_Devices_Before_The_Startup()
    {
        var testProbe = CreateTestProbe();
        var deviceManagerActor = Sys.ActorOf(DeviceManagerActor.Props());
        
        deviceManagerActor.Tell(new DeviceMessages.RequestDeviceNameList(), testProbe.Ref);
        var repliedMessage = testProbe.ExpectMsg<DeviceMessages.ReplyDeviceNameList>();
        Assert.Empty(repliedMessage.DeviceNames);
    }
    
    [Fact]
    public void Device_Manager_Should_Successfully_Start_A_Device()
    {
        var testProbe = CreateTestProbe();
        var deviceManagerActor = Sys.ActorOf(DeviceManagerActor.Props());
        var websocketManagerActor = Sys.ActorOf(WebSocketManagerActor.Props());
        
        deviceManagerActor.Tell(new DeviceMessages.StartDevice("device-1", websocketManagerActor), testProbe.Ref);
        testProbe.ExpectMsg<DeviceMessages.DeviceStarted>();
        var sender = testProbe.LastSender;

        deviceManagerActor.Tell(new DeviceMessages.RequestDeviceNameList(), testProbe.Ref);
        var repliedNameList = testProbe.ExpectMsg<DeviceMessages.ReplyDeviceNameList>();
        Assert.Contains("device-1", repliedNameList.DeviceNames);
        
        deviceManagerActor.Tell(new DeviceMessages.RequestActorRefList(), testProbe.Ref);
        var repliedRefList = testProbe.ExpectMsg<DeviceMessages.ReplyActorRefList>();
        Assert.Contains(sender, repliedRefList.ActorRefs);
    }

    [Fact]
    public void Device_Manager_Should_Successfully_Stop_A_Device()
    {
        var testProbe = CreateTestProbe();
        var deviceManagerActor = Sys.ActorOf(DeviceManagerActor.Props());
        var websocketManagerActor = Sys.ActorOf(WebSocketManagerActor.Props());
        
        deviceManagerActor.Tell(new DeviceMessages.StartDevice("device-1", websocketManagerActor), testProbe.Ref);
        testProbe.ExpectMsg<DeviceMessages.DeviceStarted>();
        var firstSender = testProbe.LastSender;
        
        deviceManagerActor.Tell(new DeviceMessages.StartDevice("device-2", websocketManagerActor), testProbe.Ref);
        testProbe.ExpectMsg<DeviceMessages.DeviceStarted>();
        var secondSender = testProbe.LastSender;

        firstSender.Should().NotBe(secondSender);
        
        deviceManagerActor.Tell(new DeviceMessages.RequestDeviceNameList(), testProbe.Ref);
        var repliedNameList = testProbe.ExpectMsg<DeviceMessages.ReplyDeviceNameList>();

        Assert.Contains("device-1", repliedNameList.DeviceNames);
        Assert.Contains("device-2", repliedNameList.DeviceNames);
        
        deviceManagerActor.Tell(new DeviceMessages.StopDevice("device-2"), testProbe.Ref);
        testProbe.ExpectMsg<DeviceMessages.DeviceStopped>();

        testProbe.LastSender.Should().Be(secondSender);
        
        AwaitAssert(() =>
        {
            deviceManagerActor.Tell(new DeviceMessages.RequestDeviceNameList(), testProbe.Ref);
            var repliedNameListAfterStop = testProbe.ExpectMsg<DeviceMessages.ReplyDeviceNameList>();
        
            Assert.DoesNotContain("device-2", repliedNameListAfterStop.DeviceNames);
        }, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Device_Manager_Should_Not_Repeatedly_Start_A_Device()
    {
        var testProbe = CreateTestProbe();
        var deviceManagerActor = Sys.ActorOf(DeviceManagerActor.Props());
        var websocketManagerActor = Sys.ActorOf(WebSocketManagerActor.Props());
        
        deviceManagerActor.Tell(new DeviceMessages.StartDevice("device-1", websocketManagerActor), testProbe.Ref);
        testProbe.ExpectMsg<DeviceMessages.DeviceStarted>();
        
        deviceManagerActor.Tell(new DeviceMessages.StartDevice("device-1", websocketManagerActor), testProbe.Ref);
        testProbe.ExpectMsg<ErrorMessages.DeviceAlreadyOnline>();
    }

    [Fact]
    public void Device_Manager_Should_Not_Stop_Not_Existent_Devices()
    {
        var testProbe = CreateTestProbe();
        var deviceManagerActor = Sys.ActorOf(DeviceManagerActor.Props());
        
        deviceManagerActor.Tell(new DeviceMessages.StopDevice("device-1"), testProbe.Ref);
        testProbe.ExpectMsg<ErrorMessages.DeviceAlreadyOffline>();
    }
}