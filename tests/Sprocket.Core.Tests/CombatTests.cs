using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Door = Sprocket.Sim.Machine.Door;
using Turret = Sprocket.Sim.Machine.Turret;
using Warhead = Sprocket.Sim.Machine.Warhead;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A turret, a warhead and a remote control as Sprocket offers them: the game's own members in Sprocket's words, the order
/// kept apart from the value it sets, Sprocket's own values read by an <c>if</c> and a <c>when</c>, and what Sprocket vouches for.
/// </summary>
public class CombatTests
{
    /// <summary>A turret, a warhead, a remote control and a door, with a program on them and a runner ticking it.</summary>
    sealed class Ship
    {
        public readonly Rigged Rig = new();
        public readonly Automation Program = new();
        public readonly Turret Turret;
        public readonly Warhead Warhead;
        public readonly Door Door;
        public readonly Remote Remote;
        public AutomationRunner Runner = null!;

        int _frame;

        public Ship()
        {
            Turret = Rig.Turret(1, "Turret");
            Warhead = Rig.Warhead(2, "Warhead");
            Door = Rig.Door(3, "Door");
            Remote = Rig.RemoteControl(4, "Remote");
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
            var off = Given.Power(false);
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

        Assert.True(kind.FindAction("Shoot", ReachKind.Value)!.Hidden);
        Assert.Equal("open fire", Listed(kind, "Shoot_On").Text);
        Assert.Equal("cease fire", Listed(kind, "Shoot_Off").Text);
        Assert.Equal("toggle firing", Listed(kind, "Shoot").Text);
        Assert.Equal("shoot once", Listed(kind, "ShootOnce").Text);
    }

    /// <summary>A target switch reads as a state and is flipped on its own switch, and its toggle and pair are said by the value.</summary>
    [Fact]
    public void ATargetSwitchReadsAsAState()
    {
        var ship = new Ship();
        var kind = ship.Kind(ship.Turret);

        var value = kind.FindAction("TargetNeutrals", ReachKind.Value)!;
        Assert.False(value.Hidden);
        Assert.Equal("Target neutrals", value.Named);
        Assert.Equal("toggle target neutrals", Flipped(kind, "TargetNeutrals").Text);
        Assert.True(kind.FindAction("TargetNeutrals_On", ReachKind.Action)!.Hidden);
        Assert.True(kind.FindAction("FocusLockedTarget", ReachKind.Action)!.Hidden);
    }

    /// <summary>A gun's switch for what it feeds from is flipped on its own switch, in the gun's words.</summary>
    [Fact]
    public void AGunKeepsItsConveyorToggle()
    {
        var ship = new Ship();
        var kind = ship.Kind(ship.Turret);

        Assert.Equal("toggle automatic push and pull", Flipped(kind, "UseConveyor").Text);
        Assert.Equal("Automatic push and pull", kind.FindAction("UseConveyor", ReachKind.Value)!.Named);
    }

    /// <summary>A turret's own values are Sprocket's, and one that is not the game's gun object has no rounds to count.</summary>
    [Fact]
    public void ATurretsOwnValuesAreSprockets()
    {
        var ship = new Ship();
        var kind = ship.Kind(ship.Turret);

        foreach (var id in new[] { "firing", "targeting", "on_target", "manually_controlled" })
            Assert.NotNull(Probe.Value(kind, BlockKinds.Own, id));

        Assert.Null(Probe.Value(kind, BlockKinds.Own, "rounds_left"));
    }

    /// <summary>Whether a gun is firing is a state of the gun, read as the gun itself beside its order to open or cease fire.</summary>
    [Fact]
    public void FiringIsAStateOfTheGun()
    {
        var ship = new Ship();
        var firing = Probe.Value(ship.Kind(ship.Turret), BlockKinds.Own, "firing")!;

        Assert.True(firing.IsStatus);
        Assert.Equal("firing", firing.Text);
    }

    /// <summary>An <c>if</c> reads each of a turret's own flags as the turret holds it, and a warhead counting down or armed as it is.</summary>
    [Theory]
    [InlineData("targeting", true)]
    [InlineData("targeting", false)]
    [InlineData("on_target", true)]
    [InlineData("on_target", false)]
    [InlineData("manually_controlled", true)]
    [InlineData("manually_controlled", false)]
    [InlineData("firing", true)]
    [InlineData("firing", false)]
    [InlineData("counting_down", true)]
    [InlineData("counting_down", false)]
    [InlineData("armed", true)]
    [InlineData("armed", false)]
    public void AnIfReadsEachOfATurretsFlagsAndAWarheadsState(string id, bool holds)
    {
        var ship = new Ship();
        string target;
        if (id == "counting_down" || id == "armed")
        {
            if (id == "counting_down")
                ship.Warhead.CountingDown = holds;
            else
                ship.Warhead.Armed = holds;

            target = ship.Name(ship.Warhead);
        }
        else
        {
            ship.Turret.Targeting = id == "targeting" && holds;
            ship.Turret.OnTarget = id == "on_target" && holds;
            ship.Turret.Controlled = id == "manually_controlled" && holds;
            ship.Turret.Firing = id == "firing" && holds;
            target = ship.Name(ship.Turret);
        }

        var cycle = ship.Program.AddSequence((Trigger)null!);
        var test = new Comparison { Provider = BlockKinds.Own, MemberId = id, Relation = Relation.Is, Value = Literal.Of(true) };
        test.Targets.Add(target);
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
        var reset = new Act("reset_targeting") { Form = ReachKind.Native };
        reset.Targets.Add(ship.Name(ship.Turret));
        cycle.Steps.Add(reset);

        ship.Start();
        ship.Runner.Call(cycle);
        ship.Tick(5);

        Assert.Contains("Turret:reset targeting", ship.Rig.Rig.Log);
    }

    /// <summary>A warhead's members are said in Sprocket's words: armed, flipped on its own switch, and its countdown.</summary>
    [Fact]
    public void AWarheadIsSaidInSprocketsWords()
    {
        var ship = new Ship();
        var kind = ship.Kind(ship.Warhead);

        var armed = kind.FindAction("armed", ReachKind.Value)!;
        Assert.True(armed.IsStatus);
        Assert.NotNull(armed.OwnValue);
        Assert.True(kind.FindAction("Safety", ReachKind.Value)!.Hidden);
        Assert.Equal("arm", Listed(kind, "Safety_On").Text);
        Assert.Equal("disarm", Listed(kind, "Safety_Off").Text);
        Assert.Equal("Detonation time", kind.FindAction("DetonationTime", ReachKind.Value)!.Named);
        Assert.Equal("start countdown", Listed(kind, "StartCountdown").Text);
        Assert.Equal("detonate", Listed(kind, "Detonate").Text);
        Assert.True(kind.FindAction("IncreaseDetonationTime", ReachKind.Action)!.Hidden);
    }

    /// <summary>A turret vouches for what it keeps, and not for the pair a target switch's value says, nor what it hides.</summary>
    [Fact]
    public void ATurretVouchesForWhatItKeeps()
    {
        var ship = new Ship();
        var kind = ship.Kind(ship.Turret);

        Vouched(kind, ReachKind.Action, "Shoot_On", "Shoot_Off", "Shoot", "ShootOnce", "UseConveyor", "TargetNeutrals");
        Vouched(kind, ReachKind.Value, "UseConveyor", "TargetNeutrals", "Range", "firing", "targeting", "on_target",
            "manually_controlled");
        Vouched(kind, ReachKind.Native, "reset_targeting");

        Unvouched(kind, ReachKind.Action, "TargetNeutrals_On", "TargetNeutrals_Off", "FocusLockedTarget");
        Unvouched(kind, ReachKind.Value, "Shoot");
    }

    /// <summary>A warhead vouches for what it keeps, and not for the pair its armed value says, nor the steps of its countdown.</summary>
    [Fact]
    public void AWarheadVouchesForWhatItKeeps()
    {
        var ship = new Ship();
        var kind = ship.Kind(ship.Warhead);

        Vouched(kind, ReachKind.Action, "StartCountdown", "StopCountdown", "Detonate", "Safety_On", "Safety_Off");
        Vouched(kind, ReachKind.Value, "armed", "DetonationTime", "counting_down");

        Unvouched(kind, ReachKind.Action, "IncreaseDetonationTime", "DecreaseDetonationTime");
    }

    /// <summary>
    /// A remote control vouches for its autopilot and for the flight switches it shares with a cockpit, in the game's words,
    /// and not for the pairs those switches' values say, the steps of its speed limit, nor the setters its values stand for;
    /// it has no main cockpit or target locking, which the game's terminal shows on a cockpit alone.
    /// </summary>
    [Fact]
    public void ARemoteControlVouchesForWhatItKeeps()
    {
        var ship = new Ship();
        var kind = ship.Kind(ship.Remote);
        var switches = new[]
        {
            "CollisionAvoidance", "HandBrake", "Park", "DampenersOverride", "ControlThrusters", "ControlWheels", "ControlGyros",
            "MainRemoteControl", "HorizonIndicator"
        };

        Vouched(kind, ReachKind.Action, "AutoPilot_On", "AutoPilot_Off", "AutoPilot", "Reset");
        Vouched(kind, ReachKind.Action, switches);
        Vouched(kind, ReachKind.Value, switches);
        Vouched(kind, ReachKind.Value, "SpeedLimit", "FlightMode", "autopilot", "current_waypoint", "speed");

        Unvouched(kind, ReachKind.Action, "CollisionAvoidance_On", "CollisionAvoidance_Off", "HandBrake_On", "HandBrake_Off", "Park_On",
            "Park_Off", "DampenersOverride_On", "DampenersOverride_Off", "IncreaseSpeedLimit", "DecreaseSpeedLimit",
            "SetFlightMode_BlockPropertyTitle_FlightMode_Patrol", "Forward");
        Unvouched(kind, ReachKind.Value, "AutoPilot");

        foreach (var id in new[] { "MainCockpit", "TargetLocking" })
        {
            Assert.Null(kind.FindAction(id, ReachKind.Value));
            Assert.Null(kind.FindAction(id, ReachKind.Action));
        }
    }

    /// <summary>Asserts that each of <paramref name="ids" /> is on the kind in that form and is vouched for.</summary>
    static void Vouched(EntityKind kind, ReachKind form, params string[] ids)
    {
        foreach (var id in ids)
        {
            var member = kind.FindAction(id, form);
            Assert.True(member != null, id + " is not on the block as a " + form);
            Assert.True(member.Vetted, id + " is not vouched for as a " + form);
        }
    }

    /// <summary>Asserts that each of <paramref name="ids" /> is on the kind in that form and is not vouched for.</summary>
    static void Unvouched(EntityKind kind, ReachKind form, params string[] ids)
    {
        foreach (var id in ids)
        {
            var member = kind.FindAction(id, form);
            Assert.True(member != null, id + " is not on the block as a " + form);
            Assert.False(member.Vetted, id + " is vouched for as a " + form);
        }
    }

    /// <summary>The toggle of the switch <paramref name="id" />, kept to the terminal view, since the switch's value flips it.</summary>
    static EntityAction Flipped(EntityKind kind, string id)
    {
        Assert.True(kind.FindAction(id, ReachKind.Value)!.Flips, id + " is not flipped on its own switch");
        var toggle = kind.FindAction(id, ReachKind.Action)!;
        Assert.True(toggle.Hidden, id + "'s toggle is still listed");
        return toggle;
    }

    /// <summary>One of the block's own actions, listed where somebody chooses.</summary>
    static EntityAction Listed(EntityKind kind, string id)
    {
        var action = kind.FindAction(id, ReachKind.Action)!;
        Assert.False(action.Hidden, id + " is left out of the list");
        return action;
    }
}
