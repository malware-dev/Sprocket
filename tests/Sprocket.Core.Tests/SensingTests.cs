using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;
using Xunit;
using IMyChatBroadcastControllerComponent = Sandbox.ModAPI.Ingame.IMyChatBroadcastControllerComponent;
using MyDetectedEntityInfo = Sandbox.ModAPI.Ingame.MyDetectedEntityInfo;
using MyLaserAntennaStatus = Sandbox.ModAPI.Ingame.MyLaserAntennaStatus;

namespace Sprocket.Tests;

// ReSharper disable AccessToModifiedClosure
// Each stand-in answers with what its test sets after making it, which is how a test changes what the block reads.

/// <summary>
/// Sensing, automation and communication: the sensor, the timer, the programmable block, the antennas, the beacon, the ore
/// detector, the broadcast controller, the transponder and the safe zone, each said in Sprocket's words, each value of its
/// own read off the block, and what each vouches for; and the event controller, which vouches for nothing of its own.
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

        /// <summary>Gives the block a toolbar of <paramref name="slots" /> slots on one page.</summary>
        public void Slots(int slots)
        {
            Toolbar = new Toolbar(slots);
        }
    }

    static void Vetted(EntityKind kind, ReachKind form, params string[] ids)
    {
        foreach (var id in ids)
            Assert.True(kind.FindAction(id, form)!.Vetted, id + " (" + form + ") is to be vetted");
    }

    static void NotVetted(EntityKind kind, ReachKind form, params string[] ids)
    {
        foreach (var id in ids)
            Assert.False(kind.FindAction(id, form)!.Vetted, id + " (" + form + ") is not to be vetted");
    }

    static Standing<T> Weld<T>(Rigged ship, long id, string name, Type type, Func<Stand<T>, Stand<T>>? answers = null)
        where T : class, IMyTerminalBlock
    {
        return ship.Rig.Weld(new Standing<T>(id, name, type, name.Replace(" ", ""), answers ?? (stand => stand)));
    }

    static Literal Read(GridEntity entity, string id)
    {
        var value = entity.Kind.FindAction(id, ReachKind.Value)!;
        return value.OwnValue.Gauge(entity.Block, new Literal[0], new ReaderMemory()).Read();
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

        Assert.True(kind.FindAction("detecting", ReachKind.Value)!.Announced);
        Assert.True(Read(entity, "detecting").Flag);
        detecting = false;
        Assert.False(Read(entity, "detecting").Flag);

        Assert.True(kind.FindAction("detections", ReachKind.Value)!.LookedBecause.Length > 0);
        Assert.Equal(2, Read(entity, "detections").Whole);
        things = 0;
        Assert.Equal(0, Read(entity, "detections").Whole);

        Assert.Equal("detect players", kind.FindAction("Detect Players", ReachKind.Value)!.Text);
        var toggle = kind.FindAction("Detect Players", ReachKind.Action)!;
        Assert.Equal("toggle detect players", toggle.Text);
        Assert.True(toggle.Hidden);
        Assert.True(kind.FindAction("Detect Players", ReachKind.Value)!.Flips);
        Assert.True(kind.FindAction("Detect Players_On", ReachKind.Action)!.Hidden);
        Assert.Equal("left extent", kind.FindAction("Left", ReachKind.Value)!.Text);
        Assert.True(kind.FindAction("IncreaseLeft", ReachKind.Action)!.Hidden);
        Assert.Equal("audible proximity alert", kind.FindAction("Audible Proximity Alert", ReachKind.Value)!.Text);
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
        Assert.True(kind.FindAction("counting_down", ReachKind.Value)!.LookedBecause.Length > 0);
        Assert.False(Read(entity, "counting_down").Flag);
        counting = true;
        Assert.True(Read(entity, "counting_down").Flag);

        Assert.Equal("start", kind.FindAction("Start", ReachKind.Action)!.Text);
        Assert.Equal("stop", kind.FindAction("Stop", ReachKind.Action)!.Text);
        Assert.Equal("trigger now", kind.FindAction("TriggerNow", ReachKind.Action)!.Text);
        Assert.Equal("delay", kind.FindAction("TriggerDelay", ReachKind.Value)!.Text);
        Assert.Equal("silent", kind.FindAction("Silent", ReachKind.Value)!.Text);
        Assert.Equal("toggle silent", kind.FindAction("Silent", ReachKind.Action)!.Text);
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

        Assert.Equal("start", entity.Kind.FindAction("Start", ReachKind.Action)!.Text);
        ship.World.Invoke(entity, "Start", null, ReachKind.Action);
        ship.World.Invoke(entity, "TriggerDelay", [Literal.Of(5.0)], ReachKind.Value);

        Assert.Contains("Timer:Start", ship.Rig.Log);
        Assert.Contains("Timer:TriggerDelay 5", ship.Rig.Log);
    }

    /// <summary>
    /// Nothing of an event controller's own is vetted: no capability of a particular block knows it, its terminal's members
    /// stay in the game's words, and its toolbar is one signal of the game's own, its slot chosen beside it, as on any block
    /// nothing knows.
    /// </summary>
    [Fact]
    public void NothingOfAnEventControllersOwnIsVetted()
    {
        var ship = new Rigged();
        var controller = Weld<IMyEventControllerBlock>(ship, 1, "Event Controller",
            typeof(MyObjectBuilder_EventControllerBlock));
        controller.Slots(9);
        controller
            .Carrying("ANDCheckbox", "AND gate", ValueKind.Flag)
            .Carrying("Threshold", "Threshold", ValueKind.Real, "0", "100")
            .Offering("ANDCheckbox", "AND gate On/Off")
            .Offering("ANDCheckbox_On", "AND gate On")
            .Offering("IncreaseThreshold", "Increase Threshold");
        var kind = ship.Entity(controller.Id).Kind;

        var floor = new Capabilities().Floor.Select(known => known.GetType()).ToList();
        Assert.All(kind.Capabilities, known => Assert.Contains(known.GetType(), floor));

        var toolbar = Assert.Single(kind.Events, signal => signal.IsPressed);
        Assert.True(toolbar.IsPressed);
        Assert.False(toolbar.Vetted);
        Assert.Equal("runs a toolbar slot", toolbar.Text);
        var second = toolbar.At(new ToolbarSlot(1, 1, 2));
        Assert.Equal("runs toolbar slot 2", second.Text);
        Assert.False(second.Vetted);

        NotVetted(kind, ReachKind.Value, "ANDCheckbox", "Threshold");
        NotVetted(kind, ReachKind.Action, "ANDCheckbox", "ANDCheckbox_On", "IncreaseThreshold");
    }

    /// <summary>
    /// A programmable block says whether its script has compile errors, which is looked for, and leaves its default argument
    /// to the terminal view.
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

        Assert.True(kind.FindAction("has_compile_errors", ReachKind.Value)!.LookedBecause.Length > 0);
        Assert.True(Read(entity, "has_compile_errors").Flag);
        broken = false;
        Assert.False(Read(entity, "has_compile_errors").Flag);

        Assert.False(kind.FindAction("ConsoleCommand", ReachKind.Value)!.Vetted);
    }

    /// <summary>A step runs a programmable block's script with the argument it gives, an empty one included.</summary>
    [Fact]
    public void AStepRunsAProgrammableBlocksScript()
    {
        var ship = new Rigged();
        var ran = new List<string>();
        var block = Weld<IMyProgrammableBlock>(ship, 1, "Script Block", typeof(MyObjectBuilder_MyProgrammableBlock),
            stand => stand.Does("TryRun", arguments =>
                {
                    ran.Add((string)arguments[0]);
                    return true;
                }));

        Assert.True(ship.Entity(block.Id).Kind.FindAction(ProgrammableBlock.Run, ReachKind.Native)!.Vetted);

        Ran.Step(ship, block, ProgrammableBlock.Run, Literal.Of("dock"));
        Ran.Step(ship, block, ProgrammableBlock.Run, Literal.Of(""));

        Assert.Equal(new[] { "dock", "" }, ran);
    }

    /// <summary>A script that throws as it is run, or does not run, ends the run at its step, saying why and which block.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AScriptThatThrowsOrDoesNotRunEndsTheRun(bool throws)
    {
        var ship = new Rigged();
        var block = Weld<IMyProgrammableBlock>(ship, 1, "Script Block", typeof(MyObjectBuilder_MyProgrammableBlock),
            stand => stand.Does("TryRun", _ => throws ? throw new InvalidOperationException("over its instruction limit") : false));

        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var run = new Act(ProgrammableBlock.Run);
        run.Targets.Add(automation.Declare(ship.World, ship.Entity(block.Id)).Name);
        run.Arguments.Add(Literal.Of(""));
        sequence.Steps.Add(run);

        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);
        runner.Call(sequence);
        ship.Tick();
        runner.Tick(2);

        var fault = runner.Get(sequence)!.Fault;
        Assert.Equal(SequenceProblem.Stuck, fault.Problem);
        Assert.Equal(Cause.ScriptNotRun, fault.Because.Cause);
        Assert.Equal("Script Block", fault.Which);
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
        Assert.Equal("HUD text", beacons.FindAction("HudText", ReachKind.Value)!.Text);

        var antennas = ship.Entity(antenna.Id).Kind;
        Assert.Equal("broadcasting", antennas.FindAction("EnableBroadCast", ReachKind.Value)!.Text);
        Assert.Equal("toggle broadcasting", antennas.FindAction("EnableBroadCast", ReachKind.Action)!.Text);
        Assert.True(antennas.FindAction("EnableBroadCast_Off", ReachKind.Action)!.Hidden);
        Assert.Equal("show ship name", antennas.FindAction("ShowShipName", ReachKind.Value)!.Text);
        Assert.Equal("toggle show ship name", antennas.FindAction("ShowShipName", ReachKind.Action)!.Text);
        Assert.Equal("broadcast radius", antennas.FindAction("Radius", ReachKind.Value)!.Text);

        var detectors = ship.Entity(detector.Id).Kind;
        Assert.Equal("broadcast using antennas", detectors.FindAction("BroadcastUsingAntennas", ReachKind.Value)!.Text);
        Assert.Equal("toggle broadcast using antennas",
            detectors.FindAction("BroadcastUsingAntennas", ReachKind.Action)!.Text);
        Assert.Equal("range", detectors.FindAction("Range", ReachKind.Value)!.Text);
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

        Assert.NotNull(kind.FindAction("link", ReachKind.Value)!.RuledOutBy);
        foreach (var id in new[] { "starts_connecting", "connects", "disconnects" })
            Assert.NotNull(kind.FindEvent(id)!.RuledOutBy);

        // Leaving idle is starting to connect, and leaving connected is disconnecting.
        status = MyLaserAntennaStatus.SearchingTargetForAntenna;
        Assert.True(entity.In(kind.FindEvent("starts_connecting")!, new ReaderMemory()));
        status = MyLaserAntennaStatus.Connected;
        Assert.True(entity.In(kind.FindEvent("connects")!, new ReaderMemory()));
        Assert.False(entity.In(kind.FindEvent("disconnects")!, new ReaderMemory()));

        Assert.Equal("disconnect", kind.FindAction("Idle", ReachKind.Action)!.Text);
        Assert.Equal("permanent connection", kind.FindAction("isPerm", ReachKind.Value)!.Text);
        Assert.Equal("toggle permanent connection", kind.FindAction("isPerm", ReachKind.Action)!.Text);
        Assert.True(kind.FindAction("PasteGpsCoords", ReachKind.Action)!.Hidden);
    }

    /// <summary>A transponder sends on its channel and receives from whom its value says; the buttons that set it are left to the terminal.</summary>
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

        Assert.Equal("send signal", kind.FindAction("SendSignal", ReachKind.Action)!.Text);
        Assert.Equal("channel", kind.FindAction("Channel", ReachKind.Value)!.Text);
        Assert.Equal("receives from", kind.FindAction("ReceiveFrom", ReachKind.Value)!.Text);
        Assert.True(kind.FindAction("SetChannelValue", ReachKind.Action)!.Hidden);
        Assert.True(kind.FindAction("SetFilter_Owner", ReachKind.Action)!.Hidden);
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
            .Offering("SafeZoneCreate_On", "Enable safe zone On")
            .Offering("SafeZoneCreate_Off", "Enable safe zone Off")
            .Carrying("SafeZoneDamageCb", "Damage", ValueKind.Flag)
            .Carrying("SafeZoneXSlider", "X", ValueKind.Real, "10", "1000")
            .Carrying("UseConveyor", "Use conveyor system", ValueKind.Flag)
            .Offering("UseConveyor", "Use conveyor system On/Off");
        var connector = Weld<IMyShipConnector>(ship, 2, "Connector", typeof(MyObjectBuilder_ShipConnector))
            .Carrying("UseConveyor", "Use conveyor system", ValueKind.Flag)
            .Offering("UseConveyor", "Use conveyor system On/Off");
        var kind = ship.Entity(zone.Id).Kind;

        Assert.True(kind.FindAction("active", ReachKind.Value)!.IsStatus);
        Assert.True(kind.FindAction("SafeZoneCreate", ReachKind.Value)!.Hidden);
        Assert.Equal("activate", kind.FindAction("SafeZoneCreate_On", ReachKind.Action)!.Text);
        Assert.Equal("deactivate", kind.FindAction("SafeZoneCreate_Off", ReachKind.Action)!.Text);
        Assert.Equal("allowing damage", kind.FindAction("SafeZoneDamageCb", ReachKind.Value)!.Text);
        Assert.Equal("size X", kind.FindAction("SafeZoneXSlider", ReachKind.Value)!.Text);
        Assert.Equal("automatic push and pull", kind.FindAction("UseConveyor", ReachKind.Value)!.Text);

        var toggle = kind.FindAction("UseConveyor", ReachKind.Action)!;
        Assert.Equal("toggle automatic push and pull", toggle.Text);
        Assert.True(toggle.Hidden);
        Assert.True(kind.FindAction("UseConveyor", ReachKind.Value)!.Flips);
        Assert.True(ship.Entity(connector.Id).Kind.FindAction("UseConveyor", ReachKind.Action)!.Hidden);
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

        Assert.False(ship.Entity(controller.Id).Kind.FindAction("CustomName", ReachKind.Value)!.Hidden);
        Assert.True(ship.Entity(beacon.Id).Kind.FindAction("CustomName", ReachKind.Value)!.Hidden);
    }

    /// <summary>
    /// A sensor vouches for what it detects, the eleven switches of what it watches for with their toggles, its six extents
    /// and its alert; the switches' on and off pairs, the extents' steps and their setters are left to the terminal.
    /// </summary>
    [Fact]
    public void ASensorVetsWhatItDetectsAndWhatItWatchesFor()
    {
        var switches = new[]
        {
            "Detect Players", "Detect Small Ships", "Detect Large Ships", "Detect Stations", "Detect Floating Objects",
            "Detect Asteroids", "Detect Subgrids", "Detect Owner", "Detect Friendly", "Detect Neutral", "Detect Enemy"
        };
        var extents = new[] { "Left", "Right", "Top", "Bottom", "Front", "Back" };

        var ship = new Rigged();
        var sensor = Weld<IMySensorBlock>(ship, 1, "Sensor", typeof(MyObjectBuilder_SensorBlock));
        sensor.Carrying("Audible Proximity Alert", "Audible Proximity Alert", ValueKind.Flag);
        foreach (var one in switches)
        {
            sensor.Carrying(one, one, ValueKind.Flag)
                .Offering(one, "Toggle " + one)
                .Offering(one + "_On", one + " On")
                .Offering(one + "_Off", one + " Off");
        }

        foreach (var one in extents)
        {
            sensor.Carrying(one, one + " Extent", ValueKind.Real, "0.1", "50")
                .Offering("Increase" + one, "Increase " + one + " Extent")
                .Offering("Decrease" + one, "Decrease " + one + " Extent")
                .Offering("Set" + one, "Set " + one + " Extent", 1);
        }

        var kind = ship.Entity(sensor.Id).Kind;

        Vetted(kind, ReachKind.Value, "detecting", "detections", "Audible Proximity Alert");
        Vetted(kind, ReachKind.Value, switches);
        Vetted(kind, ReachKind.Action, switches);
        Vetted(kind, ReachKind.Value, extents);

        NotVetted(kind, ReachKind.Action, switches.Select(one => one + "_On").ToArray());
        NotVetted(kind, ReachKind.Action, switches.Select(one => one + "_Off").ToArray());
        NotVetted(kind, ReachKind.Action, extents.Select(one => "Increase" + one).ToArray());
        NotVetted(kind, ReachKind.Action, extents.Select(one => "Decrease" + one).ToArray());
        NotVetted(kind, ReachKind.Action, extents.Select(one => "Set" + one).ToArray());
    }

    /// <summary>An ore detector vouches for its reach and its sharing through antennas, with the toggle of that.</summary>
    [Fact]
    public void AnOreDetectorVetsItsReachAndItsSharing()
    {
        var ship = new Rigged();
        var detector = Weld<IMyOreDetector>(ship, 1, "Ore Detector", typeof(MyObjectBuilder_OreDetector));
        detector.Carrying("BroadcastUsingAntennas", "Broadcast using antennas", ValueKind.Flag)
            .Carrying("Range", "Range", ValueKind.Real, "1", "150")
            .Offering("BroadcastUsingAntennas", "Broadcast using antennas On/Off");
        var kind = ship.Entity(detector.Id).Kind;

        Vetted(kind, ReachKind.Value, "BroadcastUsingAntennas", "Range");
        Vetted(kind, ReachKind.Action, "BroadcastUsingAntennas");
    }

    /// <summary>A timer vouches for its countdown, its delay, its beeps and whether it is counting down; the delay's steps are left to the terminal.</summary>
    [Fact]
    public void ATimerVetsItsCountdown()
    {
        var ship = new Rigged();
        var timer = Weld<IMyTimerBlock>(ship, 1, "Timer", typeof(MyObjectBuilder_TimerBlock));
        timer.Carrying("TriggerDelay", "Delay", ValueKind.Real, "1", "3600")
            .Carrying("Silent", "Silent", ValueKind.Flag)
            .Offering("Start", "Start")
            .Offering("Stop", "Stop")
            .Offering("TriggerNow", "Trigger now")
            .Offering("Silent", "Silent On/Off")
            .Offering("IncreaseTriggerDelay", "Increase Delay")
            .Offering("DecreaseTriggerDelay", "Decrease Delay");
        var kind = ship.Entity(timer.Id).Kind;

        Vetted(kind, ReachKind.Value, "counting_down", "TriggerDelay", "Silent");
        Vetted(kind, ReachKind.Action, "Start", "Stop", "TriggerNow", "Silent");
        NotVetted(kind, ReachKind.Action, "IncreaseTriggerDelay", "DecreaseTriggerDelay");
    }

    /// <summary>
    /// A programmable block vouches for whether its script has compile errors; its default argument and the game's run,
    /// which takes its argument from a toolbar slot, are left to the terminal with the run that takes the default argument.
    /// </summary>
    [Fact]
    public void AProgrammableBlockVetsItsArgumentAndItsCompileErrors()
    {
        var ship = new Rigged();
        var block = Weld<IMyProgrammableBlock>(ship, 1, "Script Block", typeof(MyObjectBuilder_MyProgrammableBlock));
        block.Carrying("ConsoleCommand", "Argument", ValueKind.Text)
            .Offering("Run", "Run", 1)
            .Offering("RunWithDefaultArgument", "Run with default argument");
        var kind = ship.Entity(block.Id).Kind;

        Vetted(kind, ReachKind.Value, "has_compile_errors");
        NotVetted(kind, ReachKind.Value, "ConsoleCommand");
        NotVetted(kind, ReachKind.Action, "Run", "RunWithDefaultArgument");
    }

    /// <summary>A beacon vouches for its name on the HUD and its radius; the setters written beside them and the radius's steps are left to the terminal.</summary>
    [Fact]
    public void ABeaconVetsItsMarkAndItsRadius()
    {
        var ship = new Rigged();
        var beacon = ship.Rig.Beacon(1, "Beacon");
        beacon.Carrying("HudText", "Text on HUD", ValueKind.Text)
            .Carrying("Radius", "Radius", ValueKind.Real, "1", "200000")
            .Offering("SetHudText", "Set HUD text", 1)
            .Offering("SetRadius", "Set radius", 1)
            .Offering("IncreaseRadius", "Increase Radius")
            .Offering("DecreaseRadius", "Decrease Radius");
        var kind = ship.Entity(beacon.Id).Kind;

        Vetted(kind, ReachKind.Value, "HudText", "Radius");
        NotVetted(kind, ReachKind.Action, "SetHudText", "SetRadius", "IncreaseRadius", "DecreaseRadius");
    }

    /// <summary>
    /// An antenna vouches for broadcasting, its radius, its name on the HUD and showing the ship's name, with the toggles of
    /// the two switches; broadcasting's on and off pair, the setters and the radius's steps are left to the terminal.
    /// </summary>
    [Fact]
    public void AnAntennaVetsItsBroadcast()
    {
        var ship = new Rigged();
        var antenna = Weld<IMyRadioAntenna>(ship, 1, "Antenna", typeof(MyObjectBuilder_RadioAntenna));
        antenna.Carrying("EnableBroadCast", "Broadcasting", ValueKind.Flag)
            .Carrying("ShowShipName", "Show ship name", ValueKind.Flag)
            .Carrying("Radius", "Radius", ValueKind.Real, "1", "50000")
            .Carrying("HudText", "Text on HUD", ValueKind.Text)
            .Offering("EnableBroadCast", "Broadcasting On/Off")
            .Offering("EnableBroadCast_On", "Broadcasting On")
            .Offering("EnableBroadCast_Off", "Broadcasting Off")
            .Offering("ShowShipName", "Show ship name On/Off")
            .Offering("SetHudText", "Set HUD text", 1)
            .Offering("SetRadius", "Set radius", 1)
            .Offering("IncreaseRadius", "Increase Radius");
        var kind = ship.Entity(antenna.Id).Kind;

        Vetted(kind, ReachKind.Value, "EnableBroadCast", "ShowShipName", "Radius", "HudText");
        Vetted(kind, ReachKind.Action, "EnableBroadCast", "ShowShipName");
        NotVetted(kind, ReachKind.Action, "EnableBroadCast_On", "EnableBroadCast_Off", "SetHudText", "SetRadius",
            "IncreaseRadius");
    }

    /// <summary>
    /// A laser antenna vouches for its link, dropping it, its range and whether the link is permanent, with the toggle of
    /// that; connecting through the terminal's own button, which says nothing of how it went, is left to the terminal.
    /// </summary>
    [Fact]
    public void ALaserAntennaVetsItsLink()
    {
        var ship = new Rigged();
        var antenna = Weld<IMyLaserAntenna>(ship, 1, "Laser Antenna", typeof(MyObjectBuilder_LaserAntenna));
        antenna.Carrying("isPerm", "Permanent link", ValueKind.Flag)
            .Carrying("Range", "Range", ValueKind.Real, "1", "100000000")
            .Offering("Idle", "Idle")
            .Offering("isPerm", "Permanent link On/Off")
            .Offering("ConnectGPS", "Connect")
            .Offering("PasteGpsCoords", "Paste coordinates")
            .Offering("IncreaseRange", "Increase Range");
        var kind = ship.Entity(antenna.Id).Kind;

        Vetted(kind, ReachKind.Value, "link", "isPerm", "Range");
        Vetted(kind, ReachKind.Action, "Idle", "isPerm");
        NotVetted(kind, ReachKind.Action, "ConnectGPS", "PasteGpsCoords", "IncreaseRange");
    }

    /// <summary>
    /// A broadcast controller vouches for saying something, sending a saved message, a random one and its position, who
    /// hears it, using antennas with its toggle, and its sender name; the buttons that pick who hears it, the eight transmit
    /// buttons and the saved messages one by one are left to the terminal.
    /// </summary>
    [Fact]
    public void ABroadcastControllerVetsWhatItPosts()
    {
        var ship = new Rigged();
        var controller = Weld<IMyBroadcastController>(ship, 1, "Broadcast Controller",
            typeof(MyObjectBuilder_BroadcastController));
        controller.Holding(Pretend.To<IMyChatBroadcastControllerComponent>().Done());
        controller
            .Carrying("BroadcastTarget", "Broadcast to", "Owner", "0", "Faction", "1", "Everyone", "2")
            .Carrying("UseAntenna", "Use antenna", ValueKind.Flag)
            .Carrying("Message0", "Message 1", ValueKind.Text)
            .Offering("TransmitRandomMessage", "Transmit random message")
            .Offering("SendGps", "Send GPS")
            .Offering("UseAntenna", "Use antenna On/Off")
            .Offering("BroadcastTargetEveryone", "Broadcast to everyone")
            .Offering("Transmit Message 1", "Transmit message 1");
        var kind = ship.Entity(controller.Id).Kind;

        Vetted(kind, ReachKind.Native, BroadcastController.Say, BroadcastController.SendSaved);
        Vetted(kind, ReachKind.Action, "TransmitRandomMessage", "SendGps", "UseAntenna");
        Vetted(kind, ReachKind.Value, "BroadcastTarget", "UseAntenna", "CustomName");
        NotVetted(kind, ReachKind.Action, "BroadcastTargetEveryone", "Transmit Message 1");
        NotVetted(kind, ReachKind.Value, "Message0");
    }

    /// <summary>
    /// A transponder vouches for sending a signal, its channel and whom it receives from; the channel's setter and steps, the
    /// buttons that pick whom it receives from and the setter that sends on another channel are left to the terminal.
    /// </summary>
    [Fact]
    public void ATransponderVetsWhatItSendsAndHears()
    {
        var ship = new Rigged();
        var transponder = Weld<IMyTransponder>(ship, 1, "Transponder", typeof(MyObjectBuilder_TransponderBlock));
        transponder.Carrying("Channel", "Channel", ValueKind.Integer, "1", "100")
            .Carrying("ReceiveFrom", "Receive from", "Owner", "0", "Faction", "1", "Everyone", "2")
            .Offering("SendSignal", "Send signal")
            .Offering("SetChannelValue", "Set channel", 1)
            .Offering("SetChannelValue_SendSignal", "Set channel and send", 1)
            .Offering("SetFilter_Owner", "Receive from owner")
            .Offering("IncreaseChannel", "Increase Channel");
        var kind = ship.Entity(transponder.Id).Kind;

        Vetted(kind, ReachKind.Value, "Channel", "ReceiveFrom");
        Vetted(kind, ReachKind.Action, "SendSignal");
        NotVetted(kind, ReachKind.Action, "SetChannelValue", "SetChannelValue_SendSignal", "SetFilter_Owner",
            "IncreaseChannel");
    }

    /// <summary>
    /// A safe zone vouches for whether it is up, the ten things it allows, its shape, size and look, and using conveyors with
    /// its toggle.
    /// </summary>
    [Fact]
    public void ASafeZoneVetsItsZone()
    {
        var allows = new[]
        {
            "SafeZoneDamageCb", "SafeZoneShootingCb", "SafeZoneDrillingCb", "SafeZoneWeldingCb", "SafeZoneGrindingCb",
            "SafeZoneBuildingCb", "SafeZoneBuildingProjectionCb", "SafeZoneVoxelHandCb", "SafeZoneLandingGearCb",
            "SafeZoneConvertToStationCb"
        };
        var sizes = new[] { "SafeZoneSlider", "SafeZoneXSlider", "SafeZoneYSlider", "SafeZoneZSlider" };

        var ship = new Rigged();
        var zone = Weld<IMySafeZoneBlock>(ship, 1, "Safe Zone",
            typeof(ObjectBuilders.SafeZone.MyObjectBuilder_SafeZoneBlock));
        zone.Carrying("SafeZoneCreate", "Enable safe zone", ValueKind.Flag)
            .Offering("SafeZoneCreate_On", "Enable safe zone On")
            .Offering("SafeZoneCreate_Off", "Enable safe zone Off")
            .Carrying("SafeZoneShapeCombo", "Shape", "Sphere", "0", "Box", "1")
            .Carrying("SafeZoneColor", "Color", ValueKind.Color)
            .Carrying("SafeZoneTextureCombo", "Texture", "Default", "0")
            .Carrying("UseConveyor", "Use conveyor system", ValueKind.Flag)
            .Offering("UseConveyor", "Use conveyor system On/Off");
        foreach (var one in allows)
            zone.Carrying(one, one, ValueKind.Flag);
        foreach (var one in sizes)
            zone.Carrying(one, one, ValueKind.Real, "10", "1000");
        var kind = ship.Entity(zone.Id).Kind;

        Vetted(kind, ReachKind.Value, "active", "SafeZoneShapeCombo", "SafeZoneColor", "SafeZoneTextureCombo", "UseConveyor");
        Vetted(kind, ReachKind.Action, "SafeZoneCreate_On", "SafeZoneCreate_Off");
        Vetted(kind, ReachKind.Value, allows);
        Vetted(kind, ReachKind.Value, sizes);
        Vetted(kind, ReachKind.Action, "UseConveyor");
    }

    /// <summary>A gyroscope's override is a state it is in: overriding.</summary>
    [Fact]
    public void AGyroscopeIsOverriding()
    {
        var ship = new Rigged();
        var gyroscope = Weld<IMyGyro>(ship, 1, "Gyroscope", typeof(MyObjectBuilder_Gyro))
            .Carrying("Override", "Override controls", ValueKind.Flag);

        var kind = ship.Entity(gyroscope.Id).Kind;
        Assert.Equal("overriding", kind.FindAction("Override", ReachKind.Value)!.Text);
        Assert.True(kind.FindAction("Override", ReachKind.Value)!.IsStatus);
    }

    static string Said(GridEntity entity, string id)
    {
        var value = entity.Kind.FindAction(id, ReachKind.Value)!;
        return value.OwnValue.Names[(int)Read(entity, id).Whole];
    }
}
