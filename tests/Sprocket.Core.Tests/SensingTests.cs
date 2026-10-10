using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;
using Xunit;
using MyDetectedEntityInfo = Sandbox.ModAPI.Ingame.MyDetectedEntityInfo;
using MyLaserAntennaStatus = Sandbox.ModAPI.Ingame.MyLaserAntennaStatus;

namespace Sprocket.Tests;

// ReSharper disable AccessToModifiedClosure
// Each stand-in answers with what its test sets after making it, which is how a test changes what the block reads.

/// <summary>
/// Sensing, automation and communication: the sensor, the timer, the event controller, the programmable block, the antennas,
/// the beacon, the ore detector, the transponder and the safe zone, each said in Sprocket's words and each value of its own
/// read off the block.
/// </summary>
/// <remarks>
/// The harness has none of these but the timer, the programmable block and the beacon, so each stands here as the game's
/// interface answering what a test asks of it, with the terminal members the game declares for it.
/// </remarks>
public class SensingTests
{
    /// <summary>A block of the game's interface <typeparamref name="T" />, answering what a test gives it and nothing more.</summary>
    sealed class Standing<T> : Fitting where T : class, IMyTerminalBlock
    {
        public Standing(long id, string name, MyObjectBuilderType type, string subtype, Func<Stand<T>, Stand<T>> answers)
            : base(id, name, type, subtype, name)
        {
            Fit(answers(Pretend.To<T>()));
        }
    }

    static Standing<T> Weld<T>(Rigged ship, long id, string name, Type type, Func<Stand<T>, Stand<T>>? answers = null)
        where T : class, IMyTerminalBlock
    {
        return ship.Rig.Weld(new Standing<T>(id, name, type, name.Replace(" ", ""), answers ?? (stand => stand)));
    }

    static Literal Read(GridEntity entity, string id)
    {
        var value = entity.Kind.FindAction(id, Form.Value)!;
        return value.Being.Gauge(entity.Block, new Literal[0], new Recall()).Read();
    }

    /// <summary>
    /// A sensor sends its two signals by the words of the value beside them, says whether it detects and how many things,
    /// and its terminal's switches and extents are said as what it is watching for and how far.
    /// </summary>
    [Fact]
    public void ASensorSaysWhatItDetectsAndWhatItWatchesFor()
    {
        var ship = new Rigged();
        var detecting = true;
        var things = 2;
        var sensor = Weld<IMySensorBlock>(ship, 1, "Sensor", typeof(MyObjectBuilder_SensorBlock), stand => stand
                .Gives("IsActive", () => detecting)
                .Does("DetectedEntities", args =>
                {
                    var into = (List<MyDetectedEntityInfo>)args[0];
                    for (var i = 0; i < things; i++)
                        into.Add(new MyDetectedEntityInfo());
                }))
            .Carrying("Detect Players", "Detect Players", ValueKind.Flag)
            .Carrying("Left", "Left Extent", ValueKind.Real, "0.1", "50")
            .Carrying("Audible Proximity Alert", "Audible Proximity Alert", ValueKind.Flag)
            .Offering("Detect Players", "Toggle Detect Players")
            .Offering("Detect Players_On", "Detect Players On")
            .Offering("Detect Players_Off", "Detect Players Off")
            .Offering("IncreaseLeft", "Increase Left Extent");
        var entity = ship.Entity(sensor.Id);
        var kind = entity.Kind;

        Assert.Equal("starts detecting", kind.FindEvent("toolbar-1-0-1")!.Text);
        Assert.Equal("stops detecting", kind.FindEvent("toolbar-1-0-2")!.Text);

        Assert.True(kind.FindAction("detecting", Form.Value)!.Announced);
        Assert.True(Read(entity, "detecting").Flag);
        detecting = false;
        Assert.False(Read(entity, "detecting").Flag);

        Assert.True(kind.FindAction("things-detected", Form.Value)!.LookedBecause.Length > 0);
        Assert.Equal(2, Read(entity, "things-detected").Whole);
        things = 0;
        Assert.Equal(0, Read(entity, "things-detected").Whole);

        Assert.Equal("watching for people", kind.FindAction("Detect Players", Form.Value)!.Text);
        var toggle = kind.FindAction("Detect Players", Form.Action)!;
        Assert.Equal("toggle watching for people", toggle.Text);
        Assert.False(toggle.Hidden);
        Assert.True(kind.FindAction("Detect Players_On", Form.Action)!.Hidden);
        Assert.Equal("left extent", kind.FindAction("Left", Form.Value)!.Text);
        Assert.True(kind.FindAction("IncreaseLeft", Form.Action)!.Hidden);
        Assert.Equal("sounding an alert", kind.FindAction("Audible Proximity Alert", Form.Value)!.Text);
    }

    /// <summary>A timer says whether it is counting down, and its countdown and its beeps are said in Sprocket's words.</summary>
    [Fact]
    public void ATimerSaysWhetherItIsCountingDown()
    {
        var ship = new Rigged();
        var counting = false;
        var timer = Weld<IMyTimerBlock>(ship, 1, "Timer", typeof(MyObjectBuilder_TimerBlock),
                stand => stand.Gives("IsCountingDown", () => counting))
            .Carrying("TriggerDelay", "Delay", ValueKind.Real, "1", "3600")
            .Carrying("Silent", "Silent", ValueKind.Flag)
            .Offering("Start", "Start")
            .Offering("Stop", "Stop")
            .Offering("TriggerNow", "Trigger now")
            .Offering("Silent", "Silent On/Off");
        var entity = ship.Entity(timer.Id);
        var kind = entity.Kind;

        Assert.Equal("goes off", kind.FindEvent("toolbar-1-0-0")!.Text);
        Assert.True(kind.FindAction("counting-down", Form.Value)!.LookedBecause.Length > 0);
        Assert.False(Read(entity, "counting-down").Flag);
        counting = true;
        Assert.True(Read(entity, "counting-down").Flag);

        Assert.Equal("start the countdown", kind.FindAction("Start", Form.Action)!.Text);
        Assert.Equal("stop the countdown", kind.FindAction("Stop", Form.Action)!.Text);
        Assert.Equal("go off now", kind.FindAction("TriggerNow", Form.Action)!.Text);
        Assert.Equal("delay", kind.FindAction("TriggerDelay", Form.Value)!.Text);
        Assert.Equal("silent", kind.FindAction("Silent", Form.Value)!.Text);
        Assert.Equal("toggle silent", kind.FindAction("Silent", Form.Action)!.Text);
    }

    /// <summary>A renamed timer member is the game's own, and the game does it.</summary>
    [Fact]
    public void ARenamedTimerMemberDoesWhatTheGamesDoes()
    {
        var ship = new Rigged();
        var timer = ship.Rig.Timer(1, "Timer");
        timer.Offering("Start", "Start");
        timer.Carrying("TriggerDelay", "Delay", ValueKind.Real, "1", "3600");
        var entity = ship.Entity(timer.Id);

        Assert.Equal("start the countdown", entity.Kind.FindAction("Start", Form.Action)!.Text);
        ship.World.Invoke(entity, "Start", null, Form.Action);
        ship.World.Invoke(entity, "TriggerDelay", [Literal.Of(5.0)], Form.Value);

        Assert.Contains("Timer:Start", ship.Rig.Log);
        Assert.Contains("Timer:TriggerDelay 5", ship.Rig.Log);
    }

    /// <summary>An event controller's signals are said as it meets its condition and stops, and its toggle by what it flips.</summary>
    [Fact]
    public void AnEventControllerMeetsItsCondition()
    {
        var ship = new Rigged();
        var controller = Weld<IMyEventControllerBlock>(ship, 1, "Event Controller",
                typeof(MyObjectBuilder_EventControllerBlock))
            .Carrying("ANDCheckbox", "AND gate", ValueKind.Flag)
            .Offering("ANDCheckbox", "AND gate On/Off");
        var kind = ship.Entity(controller.Id).Kind;

        Assert.Equal("meets its condition", kind.FindEvent("toolbar-1-0-1")!.Text);
        Assert.Equal("stops meeting its condition", kind.FindEvent("toolbar-1-0-2")!.Text);
        Assert.Equal("toggle met by", kind.FindAction("ANDCheckbox", Form.Action)!.Text);
    }

    /// <summary>
    /// A programmable block says whether its script has compile errors, which is looked for, and offers its default argument,
    /// which a step can set.
    /// </summary>
    [Fact]
    public void AProgrammableBlockSaysWhetherItsScriptIsBroken()
    {
        var ship = new Rigged();
        var broken = true;
        var block = Weld<IMyProgrammableBlock>(ship, 1, "Script Block", typeof(MyObjectBuilder_MyProgrammableBlock),
                stand => stand.Gives("HasCompileErrors", () => broken))
            .Carrying("ConsoleCommand", "Argument", ValueKind.Text);
        var entity = ship.Entity(block.Id);
        var kind = entity.Kind;

        Assert.True(kind.FindAction("has-compile-errors", Form.Value)!.LookedBecause.Length > 0);
        Assert.True(Read(entity, "has-compile-errors").Flag);
        broken = false;
        Assert.False(Read(entity, "has-compile-errors").Flag);

        var argument = kind.FindAction("ConsoleCommand", Form.Value)!;
        Assert.Equal("default argument", argument.Text);
        Assert.False(argument.Hidden);
    }

    /// <summary>A beacon, an antenna and an ore detector say what they broadcast and how far in Sprocket's words.</summary>
    [Fact]
    public void BroadcastingIsSaidInSprocketsWords()
    {
        var ship = new Rigged();
        var beacon = ship.Rig.Beacon(1, "Beacon").Carrying("HudText", "Text on HUD", ValueKind.Text);
        var antenna = Weld<IMyRadioAntenna>(ship, 2, "Antenna", typeof(MyObjectBuilder_RadioAntenna))
            .Carrying("EnableBroadCast", "Broadcasting", ValueKind.Flag)
            .Carrying("ShowShipName", "Show ship name", ValueKind.Flag)
            .Carrying("Radius", "Radius", ValueKind.Real, "1", "50000")
            .Offering("EnableBroadCast", "Broadcasting On/Off")
            .Offering("EnableBroadCast_On", "Broadcasting On")
            .Offering("EnableBroadCast_Off", "Broadcasting Off")
            .Offering("ShowShipName", "Show ship name On/Off");
        var detector = Weld<IMyOreDetector>(ship, 3, "Ore Detector", typeof(MyObjectBuilder_OreDetector))
            .Carrying("BroadcastUsingAntennas", "Broadcast using antennas", ValueKind.Flag)
            .Carrying("Range", "Range", ValueKind.Real, "1", "150")
            .Offering("BroadcastUsingAntennas", "Broadcast using antennas On/Off");

        var beacons = ship.Entity(beacon.Id).Kind;
        Assert.Equal("name on HUD", beacons.FindAction("HudText", Form.Value)!.Text);

        var antennas = ship.Entity(antenna.Id).Kind;
        Assert.Equal("broadcasting", antennas.FindAction("EnableBroadCast", Form.Value)!.Text);
        Assert.Equal("toggle broadcasting", antennas.FindAction("EnableBroadCast", Form.Action)!.Text);
        Assert.True(antennas.FindAction("EnableBroadCast_Off", Form.Action)!.Hidden);
        Assert.Equal("showing ship name", antennas.FindAction("ShowShipName", Form.Value)!.Text);
        Assert.Equal("toggle showing ship name", antennas.FindAction("ShowShipName", Form.Action)!.Text);
        Assert.Equal("radius", antennas.FindAction("Radius", Form.Value)!.Text);

        var detectors = ship.Entity(detector.Id).Kind;
        Assert.Equal("sharing through antennas", detectors.FindAction("BroadcastUsingAntennas", Form.Value)!.Text);
        Assert.Equal("toggle sharing through antennas",
            detectors.FindAction("BroadcastUsingAntennas", Form.Action)!.Text);
        Assert.Equal("range", detectors.FindAction("Range", Form.Value)!.Text);
    }

    /// <summary>
    /// A laser antenna says how its link stands, sends a signal at each of the link's three edges, and is heard through the
    /// game's event wherever it is idle.
    /// </summary>
    [Fact]
    public void ALaserAntennaSaysHowItsLinkStands()
    {
        var ship = new Rigged();
        var status = MyLaserAntennaStatus.Idle;
        var antenna = Weld<IMyLaserAntenna>(ship, 1, "Laser Antenna", typeof(MyObjectBuilder_LaserAntenna),
                stand => stand.Gives("Status", () => status))
            .Carrying("isPerm", "Permanent link", ValueKind.Flag)
            .Offering("Idle", "Idle")
            .Offering("isPerm", "Permanent link On/Off")
            .Offering("PasteGpsCoords", "Paste coordinates");
        var entity = ship.Entity(antenna.Id);
        var kind = entity.Kind;

        Assert.Equal("idle", Said(entity, "link"));
        status = MyLaserAntennaStatus.RotatingToTarget;
        Assert.Equal("turning", Said(entity, "link"));
        status = MyLaserAntennaStatus.OutOfRange;
        Assert.Equal("out-of-range", Said(entity, "link"));

        Assert.NotNull(kind.FindAction("link", Form.Value)!.RuledOutBy);
        foreach (var id in new[] { "starts-connecting", "connects", "disconnects" })
            Assert.NotNull(kind.FindEvent(id)!.RuledOutBy);

        // Leaving idle is starting to connect, and leaving connected is disconnecting.
        status = MyLaserAntennaStatus.SearchingTargetForAntenna;
        Assert.True(entity.In(kind.FindEvent("starts-connecting")!, new Recall()));
        status = MyLaserAntennaStatus.Connected;
        Assert.True(entity.In(kind.FindEvent("connects")!, new Recall()));
        Assert.False(entity.In(kind.FindEvent("disconnects")!, new Recall()));

        Assert.Equal("disconnect", kind.FindAction("Idle", Form.Action)!.Text);
        Assert.Equal("permanent", kind.FindAction("isPerm", Form.Value)!.Text);
        Assert.Equal("toggle permanent", kind.FindAction("isPerm", Form.Action)!.Text);
        Assert.True(kind.FindAction("PasteGpsCoords", Form.Action)!.Hidden);
    }

    /// <summary>A transponder sends on its channel and hears from whom its value says; the buttons that set it are left to the terminal.</summary>
    [Fact]
    public void ATransponderSendsAndHearsInSprocketsWords()
    {
        var ship = new Rigged();
        var transponder = Weld<IMyTransponder>(ship, 1, "Transponder", typeof(MyObjectBuilder_TransponderBlock))
            .Carrying("Channel", "Channel", ValueKind.Integer, "1", "100")
            .Carrying("ReceiveFrom", "Receive from", "Owner", "0", "Faction", "1", "Everyone", "2")
            .Offering("SendSignal", "Send signal")
            .Offering("SetChannelValue", "Set channel", 1)
            .Offering("SetFilter_Owner", "Receive from owner");
        var kind = ship.Entity(transponder.Id).Kind;

        Assert.Equal("send a signal", kind.FindAction("SendSignal", Form.Action)!.Text);
        Assert.Equal("channel", kind.FindAction("Channel", Form.Value)!.Text);
        Assert.Equal("hears from", kind.FindAction("ReceiveFrom", Form.Value)!.Text);
        Assert.True(kind.FindAction("SetChannelValue", Form.Action)!.Hidden);
        Assert.True(kind.FindAction("SetFilter_Owner", Form.Action)!.Hidden);
    }

    /// <summary>
    /// A safe zone says whether it is up and what it allows, and offers flipping its conveyors, which every other block keeps
    /// to the terminal.
    /// </summary>
    [Fact]
    public void ASafeZoneSaysWhatItAllows()
    {
        var ship = new Rigged();
        var zone = Weld<IMySafeZoneBlock>(ship, 1, "Safe Zone", typeof(ObjectBuilders.SafeZone.MyObjectBuilder_SafeZoneBlock))
            .Carrying("SafeZoneCreate", "Enable safe zone", ValueKind.Flag)
            .Carrying("SafeZoneDamageCb", "Damage", ValueKind.Flag)
            .Carrying("SafeZoneXSlider", "X", ValueKind.Real, "10", "1000")
            .Carrying("UseConveyor", "Use conveyor system", ValueKind.Flag)
            .Offering("UseConveyor", "Use conveyor system On/Off");
        var connector = Weld<IMyShipConnector>(ship, 2, "Connector", typeof(MyObjectBuilder_ShipConnector))
            .Carrying("UseConveyor", "Use conveyor system", ValueKind.Flag)
            .Offering("UseConveyor", "Use conveyor system On/Off");
        var kind = ship.Entity(zone.Id).Kind;

        Assert.Equal("zone active", kind.FindAction("SafeZoneCreate", Form.Value)!.Text);
        Assert.Equal("allowing damage", kind.FindAction("SafeZoneDamageCb", Form.Value)!.Text);
        Assert.Equal("size X", kind.FindAction("SafeZoneXSlider", Form.Value)!.Text);
        Assert.Equal("using conveyors", kind.FindAction("UseConveyor", Form.Value)!.Text);

        var toggle = kind.FindAction("UseConveyor", Form.Action)!;
        Assert.Equal("toggle using conveyors", toggle.Text);
        Assert.False(toggle.Hidden);
        Assert.True(ship.Entity(connector.Id).Kind.FindAction("UseConveyor", Form.Action)!.Hidden);
    }

    /// <summary>
    /// A broadcast controller's CustomName is the sender name chat shows, which stays offered, where every other block's
    /// CustomName is its own name and is kept out.
    /// </summary>
    [Fact]
    public void ABroadcastControllersSenderNameIsOffered()
    {
        var ship = new Rigged();
        var controller = Weld<IMyBroadcastController>(ship, 1, "Broadcast Controller",
            typeof(MyObjectBuilder_BroadcastController));
        var beacon = ship.Rig.Beacon(2, "Beacon");

        Assert.False(ship.Entity(controller.Id).Kind.FindAction("CustomName", Form.Value)!.Hidden);
        Assert.True(ship.Entity(beacon.Id).Kind.FindAction("CustomName", Form.Value)!.Hidden);
    }

    static string Said(GridEntity entity, string id)
    {
        var value = entity.Kind.FindAction(id, Form.Value)!;
        return value.Being.Names[(int)Read(entity, id).Whole];
    }
}
