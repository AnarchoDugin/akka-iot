using DeviceSystemBackend.Actors.Devices;
using DeviceSystemBackend.Actors.WebSocket;
using DeviceSystemBackend.Messages;
using FluentAssertions;

namespace DeviceSystemBackendTesting;

public class DeviceNodeActorTester : Akka.TestKit.Xunit2.TestKit
{
    [Fact]
    public void Device_Node_Should_Start_And_Reply_A_Message()
    {
        var testProbe = CreateTestProbe();
        var websocketTestActor = Sys.ActorOf(WebSocketManagerActor.Props());
        var deviceTestActor = Sys.ActorOf(DeviceNodeActor.Props("device-1", websocketTestActor));
        
        deviceTestActor.Tell(new DeviceMessages.StartDevice("device-1", websocketTestActor), testProbe.Ref);
        testProbe.ExpectMsg<DeviceMessages.DeviceStarted>();
        testProbe.LastSender.Should().Be(deviceTestActor);
    }

    [Fact]
    public void Device_Nodes_Should_Start_Independently_And_Reply_A_Message()
    {
        var testProbe = CreateTestProbe();
        var websocketTestActor = Sys.ActorOf(WebSocketManagerActor.Props());
        var deviceNodeFirst = Sys.ActorOf(DeviceNodeActor.Props("device-1", websocketTestActor));
        var deviceNodeSecond = Sys.ActorOf(DeviceNodeActor.Props("device-2", websocketTestActor));
        
        deviceNodeFirst.Tell(new DeviceMessages.StartDevice("device-1", websocketTestActor), testProbe.Ref);
        testProbe.ExpectMsg<DeviceMessages.DeviceStarted>();
        var firstSender = testProbe.LastSender;
        
        deviceNodeSecond.Tell(new DeviceMessages.StartDevice("device-2", websocketTestActor), testProbe.Ref);
        testProbe.ExpectMsg<DeviceMessages.DeviceStarted>();
        var secondSender = testProbe.LastSender;

        firstSender.Should().NotBe(secondSender);
    }

    [Fact]
    public void Device_Node_Should_Stop_Without_Problems()
    {
        var testProbe = CreateTestProbe();
        var websocketTestActor = Sys.ActorOf(WebSocketManagerActor.Props());
        var deviceTestActor = Sys.ActorOf(DeviceNodeActor.Props("device-2", websocketTestActor));
        
        deviceTestActor.Tell(new DeviceMessages.StartDevice("device-2", websocketTestActor), testProbe.Ref);
        testProbe.ExpectMsg<DeviceMessages.DeviceStarted>();
        testProbe.LastSender.Should().Be(deviceTestActor);
        
        deviceTestActor.Tell(new DeviceMessages.StopDevice("device-2"), testProbe.Ref);
        testProbe.ExpectMsg<DeviceMessages.DeviceStopped>();
        testProbe.LastSender.Should().Be(deviceTestActor);
    }
}