using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// What doors, pistons, rotors and hinges say of themselves as values and signals, read and watched the way a program does.
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
            when.Does = Off();
            Program.AddSequence((Trigger)null!).System.AddTrigger(when);
        }

        /// <summary>An <c>on</c> for Sprocket's own <paramref name="signal" /> of <paramref name="block" /> that switches the lamp off.</summary>
        public void On(string signal, Fitting block)
        {
            var on = SignalTrigger.Written(BlockKinds.Own, signal);
            on.Sources.Add(Name(block));
            on.Does = Off();
            Program.AddSequence((Trigger)null!).System.AddTrigger(on);
        }

        Act Off()
        {
            var off = new Act(BlockKinds.SwitchOff) { Form = Form.Native };
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
            var kind = Rig.Entity(block.Id).Kind.FindAction(member, Form.Value)!;
            var operand = kind.Being!.Kind == ValueKind.Name ? Literal.Name(kind.Being.Names[0]) : Literal.Of(0.0);
            if (kind.Being.Kind == ValueKind.Flag)
                operand = Literal.Of(true);
            else if (kind.Being.Kind == ValueKind.Text)
                operand = Literal.Of("");

            var probe = Probe.Of(Rig.Entity(block.Id), BlockKinds.Own, member, new Literal[0], Quantifier.Any, Relation.Is, operand)!;
            return probe.Gauge.Read();
        }
    }

    /// <summary>How far a door is open reads as a percentage, and a <c>when</c> on it fires as the door gets there.</summary>
    [Fact]
    public void HowFarADoorIsOpenIsAPercentage()
    {
        var ship = new Ship();
        var door = ship.Rig.Door(1, "Door");
        ship.When("how-far-open", Relation.AtLeast, Literal.Of(50.0), door);
        ship.Start();
        Assert.Equal(0.0, ship.Read("how-far-open", door).Real);

        door.Perform("Open_On", null!);
        ship.Tick(10);
        Assert.Equal(0, ship.Fired);

        ship.Tick(30);
        Assert.Equal(1, ship.Fired);

        ship.Tick(20);
        Assert.Equal(100.0, ship.Read("how-far-open", door).Real, 3);
    }

    /// <summary>A shut door rules out its moving, so how far it is open is looked at only once it starts to open.</summary>
    [Fact]
    public void HowFarADoorIsOpenIsRuledOutWhileItIsShut()
    {
        var ship = new Ship();
        var value = ship.Rig.Entity(ship.Rig.Door(1, "Door").Id).Kind.FindAction("how-far-open", Form.Value)!;

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
        Assert.Equal(2, ship.Read("status", piston).Whole);

        piston.Perform("Retract", null!);
        Assert.Equal(3, ship.Read("status", piston).Whole);
    }

    /// <summary>A piston reaching the end of its travel is seen on the tick after, since the piston raises it.</summary>
    [Theory]
    [InlineData("finishes-extending", 9.9f, "Extend")]
    [InlineData("finishes-retracting", 0.1f, "Retract")]
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

        var add = kind.FindAction("Add Top Part", Form.Action)!;
        Assert.False(add.Hidden);
        Assert.Equal("add head", add.Text);
    }
}
