using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using VRage.Game;
using VRage.ObjectBuilders;
using Xunit;

namespace Sprocket.Tests;

/// <summary>What every block offers whatever it is, which of it Sprocket vouches for, and what the game's catch-all functional blocks add.</summary>
public class CommonMembersTests
{
    /// <summary>A lamp and a door, with a program on them and a runner ticking it.</summary>
    sealed class Ship
    {
        public readonly Rigged Rig = new();
        public readonly Automation Program = new();
        public readonly Lamp Lamp;
        public readonly Door Door;
        public AutomationRunner Runner = null!;

        public Ship()
        {
            Lamp = Rig.Light(1, "Lamp");
            Door = Rig.Door(2, "Door");
        }

        public int Frame { get; private set; }

        public int Fired => Rig.Rig.Log.Count(line => line == "Door:off");

        public string Name(Fitting fitting) => Program.Declare(Rig.World, Rig.Entity(fitting.Id)).Name;

        /// <summary>A when on Sprocket's own <paramref name="member" /> of the lamp being <paramref name="value" />, which switches the door off.</summary>
        public void When(string member, Literal value)
        {
            var when = new WhenTrigger(member) { Provider = BlockKinds.Own, Quantifier = Quantifier.Any, Relation = Relation.Is, Value = value };
            when.Sources.Add(Name(Lamp));
            when.Does = Off();
            Program.AddSequence((Trigger)null!).System.AddTrigger(when);
        }

        /// <summary>A sequence started by the lamp's signal <paramref name="signal" />, which switches the door off.</summary>
        public void On(string signal)
        {
            var on = SignalTrigger.Written(BlockKinds.Own, signal);
            on.Sources.Add(Name(Lamp));
            Program.AddSequence(on).Steps.Add(Off());
        }

        public void Start()
        {
            Program.Edited();
            Runner = new AutomationRunner(Program, Rig.World);
            Tick(23);
        }

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++Frame);
            }
        }

        /// <summary>Ticks until the door has been switched off, or a hundred ticks have gone by.</summary>
        public void TickUntilFired()
        {
            var until = Frame + 100;
            while (Frame < until && Fired == 0)
                Tick();
        }

        Act Off()
        {
            var off = new Act(BlockKinds.SwitchOff) { Form = Form.Native };
            off.Targets.Add(Name(Door));
            return off;
        }
    }

    /// <summary>Every block has the members every block shares, and each says how a change in it is heard; whether it is powered or broken waits on a later decision.</summary>
    [Fact]
    public void EveryBlockHasWhatEveryBlockShares()
    {
        var ship = new Ship();
        var kind = ship.Rig.Entity(ship.Lamp.Id).Kind;

        foreach (var id in new[] { "working", "being-hacked", "custom-data", "integrity", "damaged", "build-level" })
            Assert.NotNull(kind.FindAction(id, Form.Value));

        foreach (var id in new[] { "starts-working", "stops-working" })
            Assert.NotNull(kind.FindEvent(id));

        foreach (var id in new[] { "powered", "broken" })
            Assert.Null(kind.FindAction(id, Form.Value));

        foreach (var id in new[] { "loses-power", "gets-power", "breaks", "gets-repaired" })
            Assert.Null(kind.FindEvent(id));

        Assert.True(kind.FindAction("working", Form.Value)!.Announced);
        Assert.True(kind.FindAction("custom-data", Form.Value)!.Announced);
        Assert.True(kind.FindAction("integrity", Form.Value)!.Announced);
        Assert.NotEqual("", kind.FindAction("being-hacked", Form.Value)!.LookedBecause);
    }

    /// <summary>A block the harness gives no inventory, power or screen offers none of what those bring.</summary>
    [Fact]
    public void WhatABlockHasNotGotIsNotOffered()
    {
        var ship = new Ship();
        var kind = ship.Rig.Entity(ship.Lamp.Id).Kind;

        foreach (var id in new[] { "inventory-fill", "inventory-mass", "power-use" })
            Assert.Null(kind.FindAction(id, Form.Value));

        Assert.Null(kind.FindAction("show-text", Form.Native));
    }

    /// <summary>
    /// The log names a block by its name, and by its type, id and construct where it has no name yet, as a dedicated server's
    /// blocks have none while a world loads.
    /// </summary>
    [Fact]
    public void TheLogNamesABlockWithoutAName()
    {
        var ship = new Ship();
        Assert.Equal("Lamp", SprocketUnit.Logged(ship.Lamp.Block));

        ship.Lamp.Name = "";
        var said = SprocketUnit.Logged(ship.Lamp.Block);
        Assert.StartsWith(ship.Lamp.Block.DefinitionDisplayNameText + " " + ship.Lamp.Id, said);
    }

    /// <summary>A block that starts working is seen on the tick after, since every block announces it.</summary>
    [Fact]
    public void ABlockStartingToWorkIsSeenOnTheTickAfter()
    {
        var ship = new Ship();
        ship.Lamp.Enabled = false;
        ship.On("starts-working");
        ship.Start();
        Assert.Equal(0, ship.Fired);

        ship.Lamp.Enabled = true;
        var changed = ship.Frame;
        ship.TickUntilFired();

        Assert.Equal(1, ship.Fired);
        Assert.Equal(changed + 1, ship.Frame);
    }

    /// <summary>A block that stops working fires the signal for it, and not the one for starting.</summary>
    [Fact]
    public void ABlockStoppingWorkFiresStopsWorking()
    {
        var ship = new Ship();
        ship.On("starts-working");
        ship.Start();

        ship.Lamp.Enabled = false;
        ship.Tick(30);
        Assert.Equal(0, ship.Fired);

        var stops = new Ship();
        stops.On("stops-working");
        stops.Start();

        stops.Lamp.Enabled = false;
        stops.TickUntilFired();
        Assert.Equal(1, stops.Fired);
    }

    /// <summary>Custom Data written is seen on the tick after, since every block announces it, and a when on it fires once.</summary>
    [Fact]
    public void CustomDataWrittenIsSeenOnTheTickAfter()
    {
        var ship = new Ship();
        ship.When("custom-data", Literal.Of("go"));
        ship.Start();
        Assert.Equal(0, ship.Fired);

        ship.Lamp.CustomData = "go";
        var written = ship.Frame;
        ship.TickUntilFired();

        Assert.Equal(1, ship.Fired);
        Assert.Equal(written + 1, ship.Frame);

        ship.Lamp.CustomData = "go on";
        ship.Tick(30);
        Assert.Equal(1, ship.Fired);
    }

    /// <summary>The game's toggle of a block's switch is worded toggle on/off, and flips the switch's one value rather than being listed beside it.</summary>
    [Fact]
    public void TheSwitchToggleIsToggleOnOff()
    {
        var ship = new Ship();
        var kind = ship.Rig.Entity(ship.Lamp.Id).Kind;
        var toggle = kind.FindAction("OnOff", Form.Action)!;

        Assert.Equal("Toggle on/off", toggle.Named);
        Assert.Equal("toggle on/off", toggle.Text);

        // The switch is one value, set off, on or flipped, and the acts beside it are kept to the script and the terminal view.
        var switched = kind.FindAction("power", Form.Value)!;
        Assert.True(switched.Sets);
        Assert.Equal("OnOff", switched.FlippedBy);
        Assert.True(toggle.Hidden);
        Assert.True(kind.FindAction(BlockKinds.SwitchOn, Form.Native)!.Hidden);
        Assert.True(kind.FindAction(BlockKinds.SwitchOff, Form.Native)!.Hidden);
    }

    /// <summary>
    /// What every block shares is vetted, the switch's and the HUD mark's members with it, and the game's pairs beside the
    /// switch and the HUD mark, the switch's value, and the block's name are not.
    /// </summary>
    [Fact]
    public void WhatEveryBlockSharesIsVetted()
    {
        var ship = new Ship();
        ship.Lamp.Carrying("OnOff", "On/Off", ValueKind.Flag)
            .Carrying("ShowOnHUD", "Show on HUD", ValueKind.Flag)
            .Offering("ShowOnHUD", "Show on HUD On/Off")
            .Offering("ShowOnHUD_On", "Show on HUD On")
            .Offering("ShowOnHUD_Off", "Show on HUD Off");
        var kind = ship.Rig.Entity(ship.Lamp.Id).Kind;

        foreach (var id in new[] { "working", "being-hacked", "custom-data", "integrity", "damaged", "build-level", "power", "ShowOnHUD" })
            Assert.True(kind.FindAction(id, Form.Value)!.Vetted, id + " is not vetted");

        foreach (var id in new[] { BlockKinds.SwitchOn, BlockKinds.SwitchOff })
            Assert.True(kind.FindAction(id, Form.Native)!.Vetted, id + " is not vetted");

        foreach (var id in new[] { "OnOff", "ShowOnHUD" })
            Assert.True(kind.FindAction(id, Form.Action)!.Vetted, id + " is not vetted");

        foreach (var id in new[] { "OnOff_On", "OnOff_Off", "ShowOnHUD_On", "ShowOnHUD_Off" })
            Assert.False(kind.FindAction(id, Form.Action)!.Vetted, id + " is vetted");

        foreach (var id in new[] { "OnOff", "CustomName" })
            Assert.False(kind.FindAction(id, Form.Value)!.Vetted, id + " is vetted");
    }

    /// <summary>A separation charge's detach is vetted in the game's words.</summary>
    [Fact]
    public void ASeparationChargesDetachIsVetted()
    {
        var ship = new Ship();
        var charge = Functional(ship, 3, "Charge", "LargeBlockSeparationCharge")
            .Holding(Pretend.To<IMyDestroyBlockComponent>().Done())
            .Offering("Detach", "Detach");
        var kind = ship.Rig.Entity(charge.Id).Kind;

        var detach = kind.FindAction("Detach", Form.Action)!;
        Assert.True(detach.Vetted);
        Assert.False(detach.Renamed);
        Assert.False(kind.FindAction("OnOff_On", Form.Action)!.Vetted);
    }

    /// <summary>A micro emitter's discharge, its countdown's start and stop, and the countdown are vetted, and the countdown's steps are not.</summary>
    [Fact]
    public void AMicroEmittersDischargeAndCountdownAreVetted()
    {
        var ship = new Ship();
        var emitter = Functional(ship, 3, "Emitter", "LargeBlockPrototechMicroEmitter")
            .Holding(Pretend.To<IMyEmpEntityComponent>().Done())
            .Carrying("Countdown", "Countdown", ValueKind.Real, "0", "3600")
            .Offering("Discharge", "Discharge")
            .Offering("StartCountdown", "Start Countdown")
            .Offering("StopCountdown", "Stop Countdown")
            .Offering("IncreaseCountdown", "Increase Countdown")
            .Offering("DecreaseCountdown", "Decrease Countdown");
        var kind = ship.Rig.Entity(emitter.Id).Kind;

        foreach (var id in new[] { "Discharge", "StartCountdown", "StopCountdown" })
            Assert.True(kind.FindAction(id, Form.Action)!.Vetted, id + " is not vetted");

        Assert.True(kind.FindAction("Countdown", Form.Value)!.Vetted);

        foreach (var id in new[] { "IncreaseCountdown", "DecreaseCountdown" })
            Assert.False(kind.FindAction(id, Form.Action)!.Vetted, id + " is vetted");
    }

    /// <summary>
    /// A services terminal's anyone can use is vetted with its toggle, as a door's is, and its pair and the range sliders a
    /// player never sees are not.
    /// </summary>
    [Fact]
    public void AServicesTerminalsAnyoneCanUseIsVetted()
    {
        var ship = new Ship();
        var terminal = Functional(ship, 3, "Services", "ServicesTerminal")
            .Holding(Pretend.To<IMyRepairServiceComponent>().Done())
            .Carrying("AnyoneCanUse", "Anyone can use", ValueKind.Flag)
            .Carrying("RepairServiceRange", "Repair Range", ValueKind.Real, "0", "250")
            .Carrying("SalvageServiceRange", "Salvage Range", ValueKind.Real, "0", "250")
            .Offering("AnyoneCanUse", "Anyone can use On/Off")
            .Offering("AnyoneCanUse_On", "Anyone can use On")
            .Offering("AnyoneCanUse_Off", "Anyone can use Off");
        var kind = ship.Rig.Entity(terminal.Id).Kind;

        Assert.True(kind.FindAction("AnyoneCanUse", Form.Value)!.Vetted);
        Assert.True(kind.FindAction("AnyoneCanUse", Form.Action)!.Vetted);

        foreach (var id in new[] { "RepairServiceRange", "SalvageServiceRange" })
            Assert.False(kind.FindAction(id, Form.Value)!.Vetted, id + " is vetted");

        foreach (var id in new[] { "AnyoneCanUse_On", "AnyoneCanUse_Off" })
            Assert.False(kind.FindAction(id, Form.Action)!.Vetted, id + " is vetted");
    }

    /// <summary>A functional block with none of their components is none of those blocks, whatever members it declares.</summary>
    [Fact]
    public void TheSameMembersWithoutTheComponentAreNotVetted()
    {
        var ship = new Ship();
        var block = Functional(ship, 3, "Odd Block", "OddBlock")
            .Carrying("Countdown", "Countdown", ValueKind.Real, "0", "3600")
            .Carrying("AnyoneCanUse", "Anyone can use", ValueKind.Flag)
            .Offering("Detach", "Detach")
            .Offering("Discharge", "Discharge");
        var kind = ship.Rig.Entity(block.Id).Kind;

        foreach (var id in new[] { "Detach", "Discharge" })
            Assert.False(kind.FindAction(id, Form.Action)!.Vetted, id + " is vetted");

        foreach (var id in new[] { "Countdown", "AnyoneCanUse" })
            Assert.False(kind.FindAction(id, Form.Value)!.Vetted, id + " is vetted");
    }

    /// <summary>A step sets a block's Custom Data to the text it is given, replacing what was there.</summary>
    [Fact]
    public void AStepSetsCustomData()
    {
        var ship = new Ship();
        ship.Lamp.CustomData = "mode=idle";
        var cycle = ship.Program.AddSequence((Trigger)null!);
        cycle.Steps.Add(SetCustomData(ship, Literal.Of("mode=dock")));

        ship.Start();
        ship.Runner.Call(cycle, cycle.System.First);
        ship.Tick();

        Assert.Equal("mode=dock", ship.Lamp.CustomData);
    }

    /// <summary>Custom Data is set only to text.</summary>
    [Theory]
    [InlineData(false, SequenceProblem.None)]
    [InlineData(true, SequenceProblem.Mistyped)]
    public void CustomDataIsSetOnlyToText(bool number, SequenceProblem problem)
    {
        var ship = new Ship();
        var cycle = ship.Program.AddSequence(new EveryTrigger(600));
        cycle.Steps.Add(SetCustomData(ship, number ? Literal.Of(3.0) : Literal.Of("mode=dock")));

        ship.Start();
        Assert.Equal(problem, SequenceSummary.Checked(ship.Rig.World, ship.Runner.Bindings, cycle, cycle.System.First).Problem);
    }

    /// <summary>The programmable block the automation lives in offers its own Custom Data to be set, as every block does.</summary>
    [Fact]
    public void TheAutomationsOwnBlockOffersCustomDataToSet()
    {
        var ship = new Ship();
        var data = ship.Rig.Entity(ship.Rig.Rig.Sprocket.Id).Kind.FindAction("custom-data", Form.Value)!;

        Assert.True(data.Sets);
        Assert.True(data.Vetted);
    }

    /// <summary>A plain functional block of the game's catch-all type, as the separation charge, micro emitter and services terminal are.</summary>
    static Fitting Functional(Ship ship, long id, string name, string subtype)
    {
        return ship.Rig.Rig.Weld(new Plain(id, name, new MyObjectBuilderType(typeof(MyObjectBuilder_FunctionalBlock)), subtype, name));
    }

    /// <summary>A step setting the lamp's Custom Data to <paramref name="value" />.</summary>
    static Act SetCustomData(Ship ship, Literal value)
    {
        var set = new Act("custom-data") { Form = Form.Value, Provider = BlockKinds.Own };
        set.Targets.Add(ship.Name(ship.Lamp));
        set.Arguments.Add(value);
        return set;
    }
}
