using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A turret and a warhead as Sprocket offers them: the game's own members in Sprocket's words, the order kept apart from the
/// value it sets, and Sprocket's own values read by an <c>if</c> and a <c>when</c>.
/// </summary>
public class CombatTests
{
    /// <summary>A turret, a warhead and a door, with a program on them and a runner ticking it.</summary>
    sealed class Ship
    {
        public readonly Rigged Rig = new();
        public readonly Automation Program = new();
        public readonly Turret Turret;
        public readonly Warhead Warhead;
        public readonly Door Door;
        public AutomationRunner Runner = null!;

        int _frame;

        public Ship()
        {
            Turret = Rig.Turret(1, "Turret");
            Warhead = Rig.Warhead(2, "Warhead");
            Door = Rig.Door(3, "Door");
        }

        public string Name(Fitting fitting) => Program.Declare(Rig.World, Rig.Entity(fitting.Id)).Name;

        public EntityKind Kind(Fitting fitting) => Rig.Entity(fitting.Id).Kind;

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
                Runner.Tick(++_frame);
            }
        }

        /// <summary>A step that switches the door off, which is how a test sees that something ran.</summary>
        public Act Off()
        {
            var off = new Act(BlockKinds.SwitchOff) { Form = Form.Native };
            off.Targets.Add(Name(Door));
            return off;
        }

        public int Fired => Rig.Rig.Log.Count(line => line == "Door:off");
    }

    /// <summary>Firing is offered as the order's two actions and its toggle, in Sprocket's words, and the order's own value is left out.</summary>
    [Fact]
    public void FiringIsOfferedAsTheOrderNotItsValue()
    {
        var ship = new Ship();
        var kind = ship.Kind(ship.Turret);

        Assert.True(kind.FindAction("Shoot", Form.Value)!.Hidden);
        Assert.Equal("open fire", Listed(kind, "Shoot_On").Text);
        Assert.Equal("cease fire", Listed(kind, "Shoot_Off").Text);
        Assert.Equal("toggle firing", Listed(kind, "Shoot").Text);
        Assert.Equal("fire once", Listed(kind, "ShootOnce").Text);
    }

    /// <summary>A target switch reads as a state, its toggle is kept, and its pair is said by the value.</summary>
    [Fact]
    public void ATargetSwitchReadsAsAState()
    {
        var ship = new Ship();
        var kind = ship.Kind(ship.Turret);

        var value = kind.FindAction("TargetNeutrals", Form.Value)!;
        Assert.False(value.Hidden);
        Assert.Equal("Neutrals targeted", value.Named);
        Assert.Equal("toggle neutrals targeted", Listed(kind, "TargetNeutrals").Text);
        Assert.True(kind.FindAction("TargetNeutrals_On", Form.Action)!.Hidden);
        Assert.True(kind.FindAction("FocusLockedTarget", Form.Action)!.Hidden);
    }

    /// <summary>A gun keeps the toggle for what it feeds from, which every other block leaves to its value.</summary>
    [Fact]
    public void AGunKeepsItsConveyorToggle()
    {
        var ship = new Ship();
        var kind = ship.Kind(ship.Turret);

        Assert.Equal("toggle using conveyors", Listed(kind, "UseConveyor").Text);
        Assert.Equal("Using conveyors", kind.FindAction("UseConveyor", Form.Value)!.Named);
    }

    /// <summary>A turret's own values are Sprocket's, and one that is not the game's gun object has no rounds to count.</summary>
    [Fact]
    public void ATurretsOwnValuesAreSprockets()
    {
        var ship = new Ship();
        var kind = ship.Kind(ship.Turret);

        foreach (var id in new[] { "firing", "targeting", "on-target", "controlled-by-a-person" })
            Assert.NotNull(Probe.Value(kind, BlockKinds.Own, id));

        Assert.Null(Probe.Value(kind, BlockKinds.Own, "rounds-left"));
    }

    /// <summary>An <c>if</c> reads each of a turret's own flags as the turret holds it.</summary>
    [Theory]
    [InlineData("targeting", true)]
    [InlineData("targeting", false)]
    [InlineData("on-target", true)]
    [InlineData("on-target", false)]
    [InlineData("controlled-by-a-person", true)]
    [InlineData("controlled-by-a-person", false)]
    [InlineData("firing", true)]
    [InlineData("firing", false)]
    public void AnIfReadsEachOfATurretsFlags(string id, bool holds)
    {
        var ship = new Ship();
        ship.Turret.Targeting = id == "targeting" && holds;
        ship.Turret.OnTarget = id == "on-target" && holds;
        ship.Turret.Controlled = id == "controlled-by-a-person" && holds;
        ship.Turret.Firing = id == "firing" && holds;
        var cycle = ship.Program.AddSequence((Trigger)null!);
        var test = new Comparison { Provider = BlockKinds.Own, MemberId = id, Relation = Relation.Is, Value = Literal.Of(true) };
        test.Targets.Add(ship.Name(ship.Turret));
        cycle.Steps.Add(new If(test));
        cycle.Steps.Add(ship.Off());
        cycle.Steps.Add(new EndIf());

        ship.Start();
        ship.Runner.Call(cycle);
        ship.Tick(5);

        Assert.Equal(holds ? 1 : 0, ship.Fired);
    }

    /// <summary>A <c>when</c> on the turret targeting fires as it finds a target, and once.</summary>
    [Fact]
    public void AWhenOnTargetingFiresAsTheTurretFindsATarget()
    {
        var ship = new Ship();
        var when = new WhenTrigger("targeting") { Relation = Relation.Is, Value = Literal.Of(true), Does = ship.Off() };
        when.Sources.Add(ship.Name(ship.Turret));
        ship.Program.AddSequence((Trigger)null!).System.AddTrigger(when);
        ship.Start();
        ship.Tick(30);
        Assert.Equal(0, ship.Fired);

        ship.Turret.Targeting = true;
        ship.Tick(30);
        Assert.Equal(1, ship.Fired);
    }

    /// <summary>Resetting a turret's targeting is told to the turret.</summary>
    [Fact]
    public void ResettingTargetingIsToldToTheTurret()
    {
        var ship = new Ship();
        var cycle = ship.Program.AddSequence((Trigger)null!);
        var reset = new Act("reset-targeting") { Form = Form.Native };
        reset.Targets.Add(ship.Name(ship.Turret));
        cycle.Steps.Add(reset);

        ship.Start();
        ship.Runner.Call(cycle);
        ship.Tick(5);

        Assert.Contains("Turret:reset targeting", ship.Rig.Rig.Log);
    }

    /// <summary>A warhead's members are said in Sprocket's words: armed, its toggle, and its countdown.</summary>
    [Fact]
    public void AWarheadIsSaidInSprocketsWords()
    {
        var ship = new Ship();
        var kind = ship.Kind(ship.Warhead);

        Assert.Equal("Armed", kind.FindAction("Safety", Form.Value)!.Named);
        Assert.Equal("toggle armed", Listed(kind, "Safety").Text);
        Assert.Equal("Countdown", kind.FindAction("DetonationTime", Form.Value)!.Named);
        Assert.Equal("start countdown", Listed(kind, "StartCountdown").Text);
        Assert.Equal("detonate", Listed(kind, "Detonate").Text);
        Assert.True(kind.FindAction("Safety_On", Form.Action)!.Hidden);
        Assert.True(kind.FindAction("IncreaseDetonationTime", Form.Action)!.Hidden);
    }

    /// <summary>An <c>if</c> on a warhead counting down holds while it is.</summary>
    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void AnIfReadsAWarheadCountingDown(bool counting, int fired)
    {
        var ship = new Ship();
        ship.Warhead.CountingDown = counting;
        var cycle = ship.Program.AddSequence((Trigger)null!);
        var test = new Comparison { Provider = BlockKinds.Own, MemberId = "counting-down", Relation = Relation.Is, Value = Literal.Of(true) };
        test.Targets.Add(ship.Name(ship.Warhead));
        cycle.Steps.Add(new If(test));
        cycle.Steps.Add(ship.Off());
        cycle.Steps.Add(new EndIf());

        ship.Start();
        ship.Runner.Call(cycle);
        ship.Tick(5);

        Assert.Equal(fired, ship.Fired);
    }

    /// <summary>One of the block's own actions, listed where somebody chooses.</summary>
    static EntityAction Listed(EntityKind kind, string id)
    {
        var action = kind.FindAction(id, Form.Action)!;
        Assert.False(action.Hidden, id + " is left out of the list");
        return action;
    }
}
