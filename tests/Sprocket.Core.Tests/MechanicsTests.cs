using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// What doors, pistons, rotors and hinges say of themselves as values and signals, read and watched the way a program does,
/// and which of their members Sprocket vouches for.
/// </summary>
public class MechanicsTests
{
    /// <summary>A construct with a lamp a program switches off, and a runner ticking it.</summary>
    sealed class Ship
    {
        public readonly Rigged Rig = new();
        public readonly Automation Program = new();
        public readonly Lamp Lamp;
        public AutomationRunner Runner = null!;
        public int Frame;

        public Ship()
        {
            Lamp = Rig.Light(99, "Lamp");
        }

        public string Name(Fitting fitting) => Program.Declare(Rig.World, Rig.Entity(fitting.Id)).Name;

        /// <summary>A <c>when</c> on Sprocket's own <paramref name="member" /> of <paramref name="block" /> that switches the lamp off.</summary>
        public void When(string member, Relation relation, Literal value, Fitting block)
        {
            var when = new WhenTrigger(member) { Provider = BlockKinds.Own, Quantifier = Quantifier.Any, Relation = relation, Value = value };
            when.Sources.Add(Name(block));
            Program.AddSequence(when).Steps.Add(Off());
        }

        /// <summary>An <c>on</c> for Sprocket's own <paramref name="signal" /> of <paramref name="block" /> that switches the lamp off.</summary>
        public void On(string signal, Fitting block)
        {
            var on = SignalTrigger.Written(BlockKinds.Own, signal);
            on.Sources.Add(Name(block));
            Program.AddSequence(on).Steps.Add(Off());
        }

        Act Off()
        {
            var off = Given.Power(false);
            off.Targets.Add(Name(Lamp));
            return off;
        }

        public void Start()
        {
            Program.Edited();
            Runner = new AutomationRunner(Program, Rig.World);
            Tick(3);
        }

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++Frame);
            }
        }

        public int Fired => Rig.Rig.Log.Count(line => line == "Lamp:off");

        /// <summary>Sprocket's own <paramref name="member" /> as it reads on <paramref name="block" /> now.</summary>
        public Literal Read(string member, Fitting block)
        {
            var kind = Rig.Entity(block.Id).Kind.FindAction(member, ReachKind.Value)!;
            var operand = kind.OwnValue!.Kind == ValueKind.Name ? Literal.Name(kind.OwnValue.Names[0]) : Literal.Of(0.0);
            if (kind.OwnValue.Kind == ValueKind.Flag)
                operand = Literal.Of(true);
            else if (kind.OwnValue.Kind == ValueKind.Text)
                operand = Literal.Of("");

            var probe = Probe.Of(Rig.Entity(block.Id), new MemberKey(BlockKinds.Own, member), new Literal[0], Quantifier.Any, Relation.Is, operand)!;
            return probe.Gauge.Read();
        }
    }

    /// <summary>How far a door is open reads as a percentage, and a <c>when</c> on it fires as the door gets there.</summary>
    [Fact]
    public void HowFarADoorIsOpenIsAPercentage()
    {
        var ship = new Ship();
        var door = ship.Rig.Door(1, "Door");
        ship.When("how_far_open", Relation.AtLeast, Literal.Of(50.0), door);
        ship.Start();
        Assert.Equal(0.0, ship.Read("how_far_open", door).Real);

        door.Perform("Open_On", null!);
        ship.Tick(10);
        Assert.Equal(0, ship.Fired);

        ship.Tick(30);
        Assert.Equal(1, ship.Fired);

        ship.Tick(20);
        Assert.Equal(100.0, ship.Read("how_far_open", door).Real, 3);
    }

    /// <summary>A shut door rules out its moving, so how far it is open is looked at only once it starts to open.</summary>
    [Fact]
    public void HowFarADoorIsOpenIsRuledOutWhileItIsShut()
    {
        var ship = new Ship();
        var value = ship.Rig.Entity(ship.Rig.Door(1, "Door").Id).Kind.FindAction("how_far_open", ReachKind.Value)!;

        Assert.NotNull(value.RuledOutBy);
        Assert.False(value.Announced);
    }

    /// <summary>A piston's position and status read as the piston stands.</summary>
    [Fact]
    public void APistonSaysWhereItIsAndWhereItIsGoing()
    {
        var ship = new Ship();
        var piston = ship.Rig.Piston(1, "Piston");
        piston.Stand(4.5f);

        Assert.Equal(4.5, ship.Read("position", piston).Real, 3);
        Assert.Equal(2, ship.Read("status", piston).Integer);

        piston.Perform("Retract", null!);
        Assert.Equal(3, ship.Read("status", piston).Integer);
    }

    /// <summary>A piston reaching the end of its travel is seen on the tick after, since the piston raises it.</summary>
    [Theory]
    [InlineData("finishes_extending", 9.9f, "Extend")]
    [InlineData("finishes_retracting", 0.1f, "Retract")]
    public void APistonReachingAnEndIsSeenOnTheTickAfter(string signal, float from, string toward)
    {
        var ship = new Ship();
        var piston = ship.Rig.Piston(1, "Piston");
        piston.Stand(from);
        ship.On(signal, piston);
        ship.Start();
        piston.Perform(toward, null!);

        var reached = 0;
        while (ship.Frame < 300 && ship.Fired == 0)
        {
            var was = piston.Position;
            ship.Tick();
            if (reached == 0 && was != piston.Position && (piston.Position == 0f || piston.Position == 10f))
                reached = ship.Frame;
        }

        Assert.NotEqual(0, reached);
        Assert.InRange(ship.Frame, reached, reached + 1);
        Assert.Equal(1, piston.Listeners);
    }

    /// <summary>A rotor's angle reads in degrees round a whole turn, and a hinge's either side of straight.</summary>
    [Fact]
    public void AnAngleIsInDegrees()
    {
        var ship = new Ship();
        var rotor = ship.Rig.Rotor(1, "Rotor");
        var hinge = ship.Rig.Hinge(2, "Hinge");
        rotor.Stand(-90f);
        hinge.Stand(-45f);
        ship.Start();

        Assert.Equal(270.0, ship.Read("angle", rotor).Real, 3);
        Assert.Equal(-45.0, ship.Read("angle", hinge).Real, 3);
    }

    /// <summary>A rotor says whether it is locked, which is read rather than set.</summary>
    [Fact]
    public void ARotorSaysWhetherItIsLocked()
    {
        var ship = new Ship();
        var rotor = ship.Rig.Rotor(1, "Rotor");
        ship.Start();
        Assert.False(ship.Read("locked", rotor).Flag);

        rotor.Locked = true;
        Assert.True(ship.Read("locked", rotor).Flag);
    }

    /// <summary>A rotor's head coming off, and going back on, is each seen on the tick after, since the rotor raises it.</summary>
    [Theory]
    [InlineData("detaches", false)]
    [InlineData("attaches", true)]
    public void AHeadComingOffOrOnIsSeenOnTheTickAfter(string signal, bool attached)
    {
        var ship = new Ship();
        var rotor = ship.Rig.Rotor(1, "Rotor");
        rotor.Attached = !attached;
        ship.On(signal, rotor);
        ship.Start();
        ship.Tick(23);
        Assert.Equal(0, ship.Fired);

        rotor.Attached = attached;
        var told = ship.Frame;
        while (ship.Frame < 100 && ship.Fired == 0)
            ship.Tick();

        Assert.Equal(told + 1, ship.Frame);
        Assert.Equal(attached, ship.Read("attached", rotor).Flag);
    }

    /// <summary>A piston's head is added in the piston's own words, and is the game's own action.</summary>
    [Fact]
    public void APistonsHeadIsAddedInItsOwnWords()
    {
        var ship = new Ship();
        var kind = ship.Rig.Entity(ship.Rig.Piston(1, "Piston").Id).Kind;

        var add = kind.FindAction("Add Top Part", ReachKind.Action)!;
        Assert.False(add.Hidden);
        Assert.Equal("add piston head", add.Text);
    }

    /// <summary>
    /// A door vouches for its own open, close and readings, for the members of its terminal it keeps, and for the
    /// auto-close delay the Door Auto-Close mod adds; for none of the game's pair its own open and close stand in for, nor
    /// the switch's value they say.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ADoorVouchesForWhatItKeeps(bool hangar)
    {
        var rig = new Rigged();
        var door = hangar ? rig.HangarDoor(1, "Door") : rig.Door(1, "Door");

        // What the game's door carries beyond the harness's own, as its terminal declares them.
        door.Carrying("Open", "Open", ValueKind.Flag)
            .Carrying("AnyoneCanUse", "Anyone can use", ValueKind.Flag)
            .Offering("AnyoneCanUse", "Anyone can use On/Off")
            .Carrying("Sprocket_AutoClose", "Auto-close", ValueKind.Real, "0", "600");

        var kind = rig.Entity(door.Id).Kind;
        Vetted(kind, ReachKind.Native, "open", "close");
        Vetted(kind, ReachKind.Value, "status", "how_far_open", "AnyoneCanUse", "Sprocket_AutoClose");
        Vetted(kind, ReachKind.Action, "Open", "AnyoneCanUse");

        Unvetted(kind, ReachKind.Action, "Open_On", "Open_Off");
        Unvetted(kind, ReachKind.Value, "Open");
    }

    /// <summary>
    /// A piston vouches for its own moves and readings and for the members of its terminal it keeps, and for none of the
    /// game's moves its own stand in for, nor the setters and steps of a value it already offers.
    /// </summary>
    [Fact]
    public void APistonVouchesForWhatItKeeps()
    {
        var rig = new Rigged();
        var piston = rig.Piston(1, "Piston");

        // What the game's piston carries beyond the harness's own, as its terminal declares them.
        piston.Carrying("MaxImpulseAxis", "Max impulse axis", ValueKind.Real, "100", "1000000")
            .Carrying("MaxImpulseNonAxis", "Max impulse non-axis", ValueKind.Real, "100", "1000000")
            .Carrying("SafetyDetach", "Safety lock", ValueKind.Real, "0", "5")
            .Carrying("ShareInertiaTensor", "Share inertia tensor", ValueKind.Flag)
            .Offering("ShareInertiaTensor", "Share inertia tensor On/Off")
            .Offering("SetVelocity", "Set Velocity", 1)
            .Offering("ResetVelocity", "Reset Velocity");
        Stepped(piston, "Velocity", "UpperLimit", "LowerLimit", "MaxImpulseAxis", "MaxImpulseNonAxis", "SafetyDetach");

        var kind = rig.Entity(piston.Id).Kind;
        Vetted(kind, ReachKind.Native, Blocks.Piston.Extend, Blocks.Piston.Retract, Blocks.Piston.MoveTo);
        Vetted(kind, ReachKind.Value, "position", "status", "Velocity", "LowerLimit", "UpperLimit", "MaxImpulseAxis",
            "MaxImpulseNonAxis", "SafetyDetach", "ShareInertiaTensor");
        Vetted(kind, ReachKind.Action, "Reverse", "Add Top Part", "ShareInertiaTensor");

        Unvetted(kind, ReachKind.Action, "Extend", "Retract", "SetAndMove", "SetVelocity", "ResetVelocity");
        Unstepped(kind, "Velocity", "UpperLimit", "LowerLimit", "MaxImpulseAxis", "MaxImpulseNonAxis", "SafetyDetach");
    }

    /// <summary>
    /// A rotor and a hinge vouch for their own turn, head and readings and for the members of their terminal they keep, and
    /// for none of the game's members their own stand in for, nor the lock's value, nor the setters and steps of a value
    /// they already offer.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ARotorVouchesForWhatItKeeps(bool hinge)
    {
        var rig = new Rigged();
        var rotor = hinge ? rig.Hinge(1, "Rotor") : rig.Rotor(1, "Rotor");
        var locks = hinge ? "HingeLock" : "RotorLock";

        // What the game's rotor carries beyond the harness's own, as its terminal declares them.
        rotor.Carrying("SafetyDetach", "Safety lock", ValueKind.Real, "0", "5")
            .Carrying("ShareInertiaTensor", "Share inertia tensor", ValueKind.Flag)
            .Offering("ShareInertiaTensor", "Share inertia tensor On/Off")
            .Offering("SetVelocity", "Set Velocity", 1)
            .Offering("SetLowerLimit", "Set Lower Limit", 1)
            .Offering("SetUpperLimit", "Set Upper Limit", 1)
            .Offering("ResetVelocity", "Reset Velocity");
        Stepped(rotor, "Torque", "BrakingTorque", "Velocity", "LowerLimit", "UpperLimit", "Displacement", "SafetyDetach");

        var kind = rig.Entity(rotor.Id).Kind;
        Vetted(kind, ReachKind.Native, Blocks.Rotor.RotateTo, Blocks.Rotor.AddHead);
        Vetted(kind, ReachKind.Value, "angle", "locked", "attached", "Velocity", "LowerLimit", "UpperLimit", "Torque",
            "BrakingTorque", "Displacement", "SafetyDetach", "ShareInertiaTensor");
        Vetted(kind, ReachKind.Action, "Reverse", "Attach", "Detach", locks + "_On", locks + "_Off", locks, "ShareInertiaTensor");

        Unvetted(kind, ReachKind.Value, locks);
        Unvetted(kind, ReachKind.Action, "RotateToAngle", "SetVelocity", "SetLowerLimit", "SetUpperLimit", "ResetVelocity");
        if (hinge)
            Unvetted(kind, ReachKind.Action, "AddHingeTopPart");
        else
            Unvetted(kind, ReachKind.Action, "AddRotorTopPart", "AddSmallRotorTopPart", "AddMediumRotorTopPart");
        Unstepped(kind, "Torque", "BrakingTorque", "Velocity", "LowerLimit", "UpperLimit", "Displacement", "SafetyDetach");
    }

    /// <summary>Gives <paramref name="fitting" /> the game's step up and step down of each value <paramref name="ids" /> names, as a slider's actions are.</summary>
    static void Stepped(Fitting fitting, params string[] ids)
    {
        foreach (var id in ids)
            fitting.Offering("Increase" + id, "Increase " + id).Offering("Decrease" + id, "Decrease " + id);
    }

    /// <summary>Asserts the step up and step down of each value <paramref name="ids" /> names is not vouched for.</summary>
    static void Unstepped(EntityKind kind, params string[] ids)
    {
        foreach (var id in ids)
            Unvetted(kind, ReachKind.Action, "Increase" + id, "Decrease" + id);
    }

    /// <summary>Asserts each of <paramref name="ids" /> is on <paramref name="kind" /> as <paramref name="form" /> and vouched for.</summary>
    static void Vetted(EntityKind kind, ReachKind form, params string[] ids)
    {
        foreach (var id in ids)
        {
            var one = kind.FindAction(id, form);
            Assert.True(one != null, id + " is missing");
            Assert.True(one.Vetted, id + " is not vetted");
        }
    }

    /// <summary>Asserts each of <paramref name="ids" /> is on <paramref name="kind" /> as <paramref name="form" /> and not vouched for.</summary>
    static void Unvetted(EntityKind kind, ReachKind form, params string[] ids)
    {
        foreach (var id in ids)
        {
            var one = kind.FindAction(id, form);
            Assert.True(one != null, id + " is missing");
            Assert.False(one.Vetted, id + " is vetted");
        }
    }
}
