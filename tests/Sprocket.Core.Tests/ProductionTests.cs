using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// The production family's members on blocks that stand in for the game's: an assembler, a refinery, a sorter and a projector,
/// each answering only what a test sets.
/// </summary>
public class ProductionTests
{
    /// <summary>Registers the game's kinds of item by name, as the game does as it starts, so an item can be read from its name.</summary>
    public ProductionTests()
    {
        MyObjectBuilderType.RegisterFromAssembly(typeof(MyObjectBuilder_Ingot).Assembly);
        MyObjectBuilderType.RegisterFromAssembly(typeof(MyObjectBuilder_Assembler).Assembly);
    }

    /// <summary>An assembler or a refinery whose readings a test sets, and which raises the game's events as it is told to.</summary>
    sealed class Factory : Fitting
    {
        readonly List<Action> _producing = new();
        readonly List<Action<IMyInventory, float, float>> _volume = new();
        readonly List<Action<IMyAssembler>> _assembling = new();

        public Factory(long id, string name, bool refinery)
            : base(id, name, refinery ? new MyObjectBuilderType(typeof(MyObjectBuilder_Refinery)) : new MyObjectBuilderType(typeof(MyObjectBuilder_Assembler)),
                refinery ? "LargeRefinery" : "LargeAssembler", refinery ? "Refinery" : "Assembler")
        {
            var input = Inventory(() => InputFill, () => Iron);
            var output = Inventory(() => 0f, () => 0.0);

            if (refinery)
                Fit(Production(Pretend.To<IMyRefinery>(), input, output));
            else
                Fit(Production(Pretend.To<IMyAssembler>(), input, output)
                    .Gives("CurrentProgress", () => 0f)
                    .Gives("Mode", () => Mode)
                    .Takes("Mode", mode => Mode = (Sandbox.ModAPI.Ingame.MyAssemblerMode)mode)
                    .Gives("Repeating", () => Repeating)
                    .Takes("Repeating", repeating => Repeating = (bool)repeating)
                    .Does("add_CurrentProgressChanged", args => _assembling.Add((Action<IMyAssembler>)args[0]))
                    .Does("remove_CurrentProgressChanged", args => _assembling.Remove((Action<IMyAssembler>)args[0]))
                    .Does("add_CurrentStateChanged", args => _assembling.Add((Action<IMyAssembler>)args[0]))
                    .Does("remove_CurrentStateChanged", args => _assembling.Remove((Action<IMyAssembler>)args[0]))
                    .Does("add_CurrentModeChanged", args => _assembling.Add((Action<IMyAssembler>)args[0]))
                    .Does("remove_CurrentModeChanged", args => _assembling.Remove((Action<IMyAssembler>)args[0])));

            Offering("UseConveyor", "Use Conveyor System On/Off");
            Carrying("UseConveyor", "Use Conveyor System", ValueKind.Flag);
            if (refinery)
                return;

            Offering("slaveMode", "Cooperative mode On/Off");
            Carrying("slaveMode", "Cooperative mode", ValueKind.Flag);
        }

        public Sandbox.ModAPI.Ingame.MyAssemblerMode Mode { get; set; }

        public bool Repeating { get; set; }

        public bool Producing { get; set; }

        public bool QueueEmpty { get; set; } = true;

        public float InputFill { get; set; }

        public double Iron { get; set; }

        /// <summary>How many of the game's events are being listened to.</summary>
        public int Heard => _producing.Count + _volume.Count + _assembling.Count;

        /// <summary>Says the block has stopped producing, as the game does.</summary>
        public void Stopped()
        {
            foreach (var told in _producing.ToArray())
                told();
        }

        /// <summary>Says an inventory has changed, as the game does.</summary>
        public void Changed()
        {
            foreach (var told in _volume.ToArray())
                told(null!, 0f, 0f);
        }

        Stand<T> Production<T>(Stand<T> stand, IMyInventory input, IMyInventory output) where T : class, IMyProductionBlock
        {
            return stand
                .Gives("IsProducing", () => Producing)
                .Gives("IsQueueEmpty", () => QueueEmpty)
                .Gives("InputInventory", () => input)
                .Gives("OutputInventory", () => output)
                .Does("add_StartedProducing", args => _producing.Add((Action)args[0]))
                .Does("remove_StartedProducing", args => _producing.Remove((Action)args[0]))
                .Does("add_StoppedProducing", args => _producing.Add((Action)args[0]))
                .Does("remove_StoppedProducing", args => _producing.Remove((Action)args[0]));
        }

        IMyInventory Inventory(Func<float> fill, Func<double> iron)
        {
            return Pretend.To<IMyInventory>()
                .Gives("VolumeFillFactor", () => fill())
                .Does("GetItemAmount", args => ((VRage.Game.ModAPI.Ingame.MyItemType)args[0]).SubtypeId == "Iron"
                    ? (MyFixedPoint)iron()
                    : 0)
                .Does("add_OnVolumeChanged", args => _volume.Add((Action<IMyInventory, float, float>)args[0]))
                .Does("remove_OnVolumeChanged", args => _volume.Remove((Action<IMyInventory, float, float>)args[0]))
                .Done();
        }
    }

    /// <summary>The list of what a conveyor sorter offers says what is still to come for it, and the lamp beside it, which has nothing planned, says nothing.</summary>
    [Fact]
    public void ASortersListSaysWhatIsStillToCome()
    {
        var ship = new Ship();
        ship.Rig.Panel(3, "Airlock Button");
        ship.Weld(new Gate(1, "Sorter"));
        var editor = new SprocketEditor(ship.Rig.World, ship.Program, new AutomationRunner(ship.Program, ship.Rig.World), new Texts());

        editor.NewButtonSystem("Airlock");
        editor.PressAdd("task");
        StageColumn What() => editor.Pick().Stages.Single(stage => stage.Entries().Any(entry => entry.Text == "Power"));

        editor.PressBlock("Sorter");
        Assert.Equal("Still to come: setting the sorter's list.", What().Note);

        editor.PressBlock("Lamp");
        Assert.Equal("", What().Note);
    }

    /// <summary>A conveyor sorter, offering the game's own switches for its pulling and its filter mode.</summary>
    sealed class Gate : Fitting
    {
        public Gate(long id, string name)
            : base(id, name, new MyObjectBuilderType(typeof(MyObjectBuilder_ConveyorSorter)), "LargeBlockConveyorSorter", "Conveyor Sorter")
        {
            Fit(Pretend.To<IMyConveyorSorter>());

            Offering("DrainAll", "Drain All On/Off").Offering("DrainAll_On", "Drain All On").Offering("DrainAll_Off", "Drain All Off");
            Carrying("DrainAll", "Drain All", ValueKind.Flag);
            Carrying("blacklistWhitelist", "Mode", "Whitelist", "0", "Blacklist", "1");
        }
    }

    /// <summary>A drill whose mode a test sets, or a step does, offering the game's own switch for the mode.</summary>
    sealed class Bore : Fitting
    {
        public Bore(long id, string name)
            : base(id, name, new MyObjectBuilderType(typeof(MyObjectBuilder_Drill)), "LargeBlockDrill", "Drill")
        {
            Fit(Pretend.To<IMyShipDrill>()
                .Gives("TerrainClearingMode", () => Clearing)
                .Takes("TerrainClearingMode", clearing => Clearing = (bool)clearing)
                .Gives("IsActivated", () => Enabled));

            Offering("TerrainClearingMode", "Terrain Clearing Mode On/Off")
                .Offering("TerrainClearingMode_On", "Terrain Clearing Mode On")
                .Offering("TerrainClearingMode_Off", "Terrain Clearing Mode Off");
            Carrying("TerrainClearingMode", "Terrain Clearing Mode", ValueKind.Flag);
        }

        public bool Clearing { get; set; }
    }

    /// <summary>A store that offers the game's own access and listing switches.</summary>
    sealed class Shop : Fitting
    {
        public Shop(long id, string name)
            : base(id, name, new MyObjectBuilderType(typeof(MyObjectBuilder_StoreBlock)), "StoreBlock", "Store")
        {
            Fit(Pretend.To<IMyStoreBlock>());
            Offering("AnyoneCanUse", "Anyone Can Use On/Off");
            Carrying("AnyoneCanUse", "Anyone Can Use", ValueKind.Flag);
            Offering("Searchable", "Searchable On/Off");
            Carrying("Searchable", "Searchable", ValueKind.Flag);
        }
    }

    /// <summary>A projector whose blocks left a test sets, offering the game's own settings for where and how it shows.</summary>
    sealed class Printer : Fitting
    {
        public Printer(long id, string name)
            : base(id, name, new MyObjectBuilderType(typeof(MyObjectBuilder_Projector)), "LargeProjector", "Projector")
        {
            Fit(Pretend.To<IMyProjector>().Gives("RemainingBlocks", () => Left));

            Offering("KeepProjection", "Keep Projection On/Off");
            Carrying("KeepProjection", "Keep Projection", ValueKind.Flag);
            foreach (var placed in new[] { "X", "Y", "Z", "RotX", "RotY", "RotZ", "Scale" })
            {
                Carrying(placed, placed, ValueKind.Real);
                Offering("Increase" + placed, "Increase " + placed).Offering("Decrease" + placed, "Decrease " + placed);
            }

            foreach (var shown in new[] { "ShowOnlyBuildable", "MarkMissingBlocks", "MarkUnfinishedBlocks" })
                Carrying(shown, shown, ValueKind.Flag);
        }

        public int Left { get; set; }
    }

    /// <summary>A block of this family answering only what it is, offering what a test gives it of the game's own.</summary>
    sealed class Stood<T> : Fitting where T : class, IMyTerminalBlock
    {
        public Stood(long id, string name, Type type, string subtype, string display)
            : base(id, name, new MyObjectBuilderType(type), subtype, display)
        {
            Fit(Pretend.To<T>());
        }
    }

    /// <summary>The rig, a program on it and a runner ticking it.</summary>
    sealed class Ship
    {
        public readonly Rigged Rig = new();
        public readonly Automation Program = new();
        public readonly Lamp Lamp;
        public AutomationRunner Runner = null!;

        int _frame;

        public Ship()
        {
            Lamp = Rig.Light(9, "Lamp");
        }

        public T Weld<T>(T fitting) where T : Fitting => Rig.Rig.Weld(fitting);

        public string Name(Fitting fitting) => Program.Declare(Rig.World, Rig.Entity(fitting.Id)).Name;

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

        /// <summary>Has <paramref name="trigger" /> switch the lamp off.</summary>
        public void Switching(Trigger trigger)
        {
            var off = Given.Power(false);
            off.Targets.Add(Name(Lamp));
            trigger.Does = off;
            Program.AddSequence((Trigger)null!).System.AddTrigger(trigger);
        }

        public int Fired => Rig.Rig.Log.Count(line => line == "Lamp:off");

        /// <summary>
        /// Has a sequence of one step set Sprocket's own value <paramref name="id" /> on <paramref name="fitting" /> to
        /// <paramref name="value" />, runs it, and gives back what checking the step found.
        /// </summary>
        /// <remarks>That nothing starts the sequence is about the sequence, which is called here, and not about the step.</remarks>
        public SequenceProblem Set(Fitting fitting, string id, Literal value)
        {
            var set = new Act(id) { Form = ReachKind.Value, Provider = BlockKinds.Own };
            set.Targets.Add(Name(fitting));
            set.Arguments.Add(value);
            var sequence = Program.AddSequence((Trigger)null!);
            sequence.Steps.Add(set);
            Start();

            var problem = SequenceSummary.Checked(Rig.World, Runner.Bindings, sequence, sequence.System.First).Problem;
            Runner.Call(sequence);
            Tick(3);
            return problem == SequenceProblem.NothingStarts ? SequenceProblem.None : problem;
        }
    }

    /// <summary>
    /// An assembler offers what it is ordered to make, whether it is making it, how it is set up and what its two inventories
    /// hold, and each of its own values and signals says how it is heard.
    /// </summary>
    [Fact]
    public void AnAssemblerSaysWhatItMakesAndHolds()
    {
        var ship = new Ship();
        var kind = ship.Rig.Entity(ship.Weld(new Factory(1, "Assembler", false)).Id).Kind;

        Assert.NotNull(kind.FindAction("make", ReachKind.Native));
        Assert.NotNull(kind.FindAction("clear-the-queue", ReachKind.Native));
        foreach (var value in new[]
                 {
                     "items-queued", "progress", "producing", "stuck", "mode", "able-to-take-apart", "repeating", "input-fill",
                     "output-fill"
                 })
        {
            var own = kind.FindAction(value, ReachKind.Value);
            Assert.True(own?.OwnValue != null, value);
            Assert.True(own.Announced || own.LookedBecause.Length > 0, value);
        }

        Assert.True(kind.FindEvent("starts-producing")!.Announced);
        Assert.True(kind.FindEvent("stops-producing")!.Announced);
        Assert.Null(kind.FindAction("refining", ReachKind.Value));
    }

    /// <summary>Every signal and value of Sprocket's own on this family's blocks says how it is heard: announced or looked for, with why.</summary>
    [Fact]
    public void EverySignalAndValueSaysHowItIsHeard()
    {
        var ship = new Ship();
        var blocks = new Fitting[]
        {
            ship.Weld(new Factory(1, "Assembler", false)), ship.Weld(new Factory(2, "Refinery", true)),
            ship.Weld(new Gate(3, "Sorter")), ship.Weld(new Printer(4, "Printer"))
        };

        var unsaid = new List<string>();
        foreach (var block in blocks)
        {
            var kind = ship.Rig.Entity(block.Id).Kind;
            foreach (var signal in kind.Events)
            {
                if (!signal.Announced && signal.RuledOutBy == null && !signal.Answered && signal.LookedBecause.Length == 0)
                    unsaid.Add(kind.Id + " " + signal.Id);
            }

            foreach (var value in kind.Actions)
            {
                if (value.OwnValue != null && !value.Announced && value.RuledOutBy == null && value.LookedBecause.Length == 0)
                    unsaid.Add(kind.Id + " " + value.Id);
            }
        }

        Assert.Empty(unsaid);
    }

    /// <summary>A refinery says refining where an assembler says producing, and offers no orders, which it would ignore.</summary>
    [Fact]
    public void ARefinerySaysRefiningAndTakesNoOrders()
    {
        var ship = new Ship();
        var kind = ship.Rig.Entity(ship.Weld(new Factory(1, "Refinery", true)).Id).Kind;

        Assert.NotNull(kind.FindAction("refining", ReachKind.Value));
        Assert.NotNull(kind.FindEvent("starts-refining"));
        Assert.NotNull(kind.FindEvent("stops-refining"));
        Assert.Null(kind.FindAction("producing", ReachKind.Value));
        Assert.Null(kind.FindAction("make", ReachKind.Native));
        Assert.Null(kind.FindAction("items-queued", ReachKind.Value));
        Assert.NotNull(kind.FindAction("input-fill", ReachKind.Value));
    }

    /// <summary>
    /// The conveyor switch's own toggle is offered, in Sprocket's words, on a block of this family, and kept out of the list on a
    /// block that is not.
    /// </summary>
    [Fact]
    public void TheConveyorToggleIsOfferedOnlyOnThisFamilysBlocks()
    {
        var ship = new Ship();
        var assembler = ship.Rig.Entity(ship.Weld(new Factory(1, "Assembler", false)).Id).Kind;
        var toggle = assembler.FindAction("UseConveyor", ReachKind.Action)!;
        Assert.True(toggle.Hidden);
        Assert.True(assembler.FindAction("UseConveyor", ReachKind.Value)!.Flips);
        Assert.Equal("toggle automatic push and pull", toggle.Text);
        Assert.Equal("Automatic push and pull", assembler.FindAction("UseConveyor", ReachKind.Value)!.Named);

        var generator = new Plain(2, "Generator", new MyObjectBuilderType(typeof(MyObjectBuilder_OxygenGenerator)),
            "OxygenGenerator", "O2/H2 Generator");
        generator.Offering("UseConveyor", "Use Conveyor System On/Off");
        ship.Weld(generator);
        Assert.True(ship.Rig.Entity(generator.Id).Kind.FindAction("UseConveyor", ReachKind.Action)!.Hidden);
    }

    /// <summary>
    /// An assembler with work queued that stops producing while it is on and working is stuck, and a <c>when</c> on it is
    /// told on the tick after the game says it stopped.
    /// </summary>
    [Fact]
    public void AStuckAssemblerIsSeenOnTheTickAfterItStops()
    {
        var ship = new Ship();
        var assembler = ship.Weld(new Factory(1, "Assembler", false));
        assembler.Producing = true;
        assembler.QueueEmpty = false;

        var when = new WhenTrigger("stuck")
        {
            Provider = BlockKinds.Own, Quantifier = Quantifier.Any, Relation = Relation.Is, Value = Literal.Of(true)
        };
        when.Sources.Add(ship.Name(assembler));
        ship.Switching(when);
        ship.Start();
        ship.Tick(20);
        Assert.Equal(0, ship.Fired);
        Assert.True(assembler.Heard > 0);

        assembler.Producing = false;
        assembler.Stopped();
        ship.Tick();

        Assert.Equal(1, ship.Fired);
    }

    /// <summary>Work queued and nothing made is not stuck on a block switched off: that is the switch, not the work.</summary>
    [Theory]
    [InlineData(false, false, true, true)]
    [InlineData(true, false, true, false)]
    [InlineData(false, true, true, false)]
    [InlineData(false, false, false, false)]
    public void StuckIsWorkWaitingOnABlockThatCouldDoIt(bool emptyQueue, bool producing, bool on, bool stuck)
    {
        var ship = new Ship();
        var assembler = ship.Weld(new Factory(1, "Assembler", false));
        assembler.QueueEmpty = emptyQueue;
        assembler.Producing = producing;
        assembler.Enabled = on;

        var value = ship.Rig.Entity(assembler.Id).Kind.FindAction("stuck", ReachKind.Value)!;
        Assert.Equal(Literal.Of(stuck), value.OwnValue.Gauge(assembler.Block, new Literal[0], null).Read());
    }

    /// <summary>How full an inventory is reads as a percentage, as fill does everywhere in the game's terminal.</summary>
    [Fact]
    public void FillReadsAsAPercentage()
    {
        var ship = new Ship();
        var assembler = ship.Weld(new Factory(1, "Assembler", false));
        assembler.InputFill = 0.4f;

        var fill = ship.Rig.Entity(assembler.Id).Kind.FindAction("input-fill", ReachKind.Value)!;
        Assert.Equal(40.0, fill.OwnValue.Gauge(assembler.Block, new Literal[0], null).Read().Real, 3);
    }

    // The amount of one item in a production block's inventory waits for after the beta, with the value it tests.
    // /// <summary>How much of an item an inventory holds is a function of the item, heard as the inventory changes.</summary>
    // [Fact]
    // public void TheAmountOfAnItemIsHeardAsTheInventoryChanges()
    // {
    //     var ship = new Ship();
    //     var assembler = ship.Weld(new Factory(1, "Assembler", false));
    //
    //     var when = new WhenTrigger("amount-in-input")
    //     {
    //         Provider = BlockKinds.Own, Quantifier = Quantifier.Any, Relation = Relation.AtLeast, Value = Literal.Of(100L)
    //     };
    //     when.Arguments.Add(Literal.Of("Ingot/Iron"));
    //     when.Sources.Add(ship.Name(assembler));
    //     ship.Switching(when);
    //     ship.Start();
    //     ship.Tick(20);
    //     Assert.Equal(0, ship.Fired);
    //
    //     assembler.Iron = 250;
    //     assembler.Changed();
    //     ship.Tick();
    //
    //     Assert.Equal(1, ship.Fired);
    // }

    /// <summary>An item is written as its type and subtype, and one the game has no type for names nothing.</summary>
    [Theory]
    [InlineData("Ingot/Iron", true)]
    [InlineData("Ore/Ice", true)]
    [InlineData("Ingot", false)]
    [InlineData("Ingot/", false)]
    [InlineData("Nonsense/Iron", false)]
    public void AnItemIsItsTypeAndSubtype(string said, bool known)
    {
        Assert.Equal(known, ItemIds.Read(said, out _));
    }

    /// <summary>A drill's mode is one of mining and clearing terrain, read off the game's own switch for it.</summary>
    [Fact]
    public void ADrillsModeIsMiningOrClearingTerrain()
    {
        var ship = new Ship();
        var drill = ship.Weld(new Bore(1, "Drill"));
        var mode = ship.Rig.Entity(drill.Id).Kind.FindAction("mode", ReachKind.Value)!;

        Assert.Equal(new[] { "mining", "clearing-terrain" }, mode.OwnValue.Names);
        Assert.Equal(Literal.Of(0L), mode.OwnValue.Gauge(drill.Block, new Literal[0], null).Read());
        drill.Clearing = true;
        Assert.Equal(Literal.Of(1L), mode.OwnValue.Gauge(drill.Block, new Literal[0], null).Read());
    }

    /// <summary>Who may use a store is offered with its toggle, in Sprocket's words; another block's toggle stays out of the list.</summary>
    [Fact]
    public void TheAccessToggleIsOfferedOnAStore()
    {
        var ship = new Ship();
        var store = ship.Rig.Entity(ship.Weld(new Shop(1, "Store")).Id).Kind;
        var toggle = store.FindAction("AnyoneCanUse", ReachKind.Action)!;
        Assert.True(toggle.Hidden);
        Assert.True(store.FindAction("AnyoneCanUse", ReachKind.Value)!.Flips);
        Assert.Equal("toggle anyone can use", toggle.Text);

        var panel = new Plain(2, "Panel", new MyObjectBuilderType(typeof(MyObjectBuilder_ButtonPanel)), "ButtonPanelLarge",
            "Button Panel");
        panel.Offering("AnyoneCanUse", "Anyone Can Use On/Off");
        ship.Weld(panel);
        Assert.True(ship.Rig.Entity(panel.Id).Kind.FindAction("AnyoneCanUse", ReachKind.Action)!.Hidden);
    }

    /// <summary>A projector finishes building as its blocks left come to nothing, looked for on its cadence.</summary>
    [Fact]
    public void AProjectorFinishesBuildingAsNothingIsLeft()
    {
        var ship = new Ship();
        var printer = ship.Weld(new Printer(1, "Printer"));
        printer.Left = 12;

        var finished = SignalTrigger.Written(BlockKinds.Own, "finishes-building");
        finished.Sources.Add(ship.Name(printer));
        ship.Switching(finished);
        ship.Start();
        ship.Tick(30);
        Assert.Equal(0, ship.Fired);

        printer.Left = 0;
        ship.Tick(30);

        Assert.Equal(1, ship.Fired);
    }

    /// <summary>An item chosen from a list is one of the items listed, and the argument of a function asking for one is held to it.</summary>
    [Fact]
    public void AnItemChosenFromAListIsOneOfThem()
    {
        var item = ItemIds.Choosing("item", new[] { "Iron Ingot" }, new[] { "Ingot/Iron" });

        var function = OwnValue.Flag<IMyTerminalBlock, Item>(Item.First, (_, _, _) => true).Taking(item);
        Assert.Null(function.Refused(new[] { Literal.Of("Ingot/Iron") }));
        Assert.Equal("item", function.Refused(new[] { Literal.Of("Ingot/Gold") }));
    }

    /// <summary>Where no item is listed, as where the game has loaded none, an item is any written out.</summary>
    [Fact]
    public void AnItemWithNothingListedIsAnyWrittenOut()
    {
        var item = ItemIds.Choosing("item", new string[0], new string[0]);
        var function = OwnValue.Flag<IMyTerminalBlock, Item>(Item.First, (_, _, _) => true).Taking(item);

        Assert.Null(function.Refused(new[] { Literal.Of("Ingot/Gold") }));
        Assert.Equal("item", function.Refused(new[] { Literal.Of(3L) }));
    }

    /// <summary>
    /// Every member an assembler and a refinery keep is vetted: Sprocket's own, and the game's switches for cooperative mode
    /// and the conveyors in Sprocket's words.
    /// </summary>
    [Fact]
    public void WhatAnAssemblerAndARefineryKeepIsVetted()
    {
        var ship = new Ship();
        var assembler = ship.Rig.Entity(ship.Weld(new Factory(1, "Assembler", false)).Id).Kind;
        Vetted(assembler, ReachKind.Native, "make", "clear-the-queue");
        Vetted(assembler, ReachKind.Value, "items-queued", "progress", "producing", "stuck", "mode", "able-to-take-apart", "repeating",
            "input-fill", "output-fill", "slaveMode", "UseConveyor");
        Vetted(assembler, ReachKind.Action, "slaveMode", "UseConveyor");

        var refinery = ship.Rig.Entity(ship.Weld(new Factory(2, "Refinery", true)).Id).Kind;
        Vetted(refinery, ReachKind.Value, "refining", "stuck", "input-fill", "output-fill", "UseConveyor");
        Vetted(refinery, ReachKind.Action, "UseConveyor");
    }

    /// <summary>
    /// A sorter's filter mode and pulling are vetted, and the game's own pull on and pull off beside its pulling are not; its
    /// list waits for the setting of it that is still to come.
    /// </summary>
    [Fact]
    public void WhatASorterKeepsIsVettedAndItsPullSwitchesAreNot()
    {
        var ship = new Ship();
        var sorter = ship.Rig.Entity(ship.Weld(new Gate(1, "Sorter")).Id).Kind;

        Assert.Equal(new[] { PlannedMember.SorterList }, sorter.Planned);
        Vetted(sorter, ReachKind.Value, "blacklistWhitelist", "DrainAll");
        Vetted(sorter, ReachKind.Action, "DrainAll");
        Unvetted(sorter, ReachKind.Action, "DrainAll_On", "DrainAll_Off");
    }

    /// <summary>
    /// A drill's own values and the flip of its mode are vetted, and the game's own terrain clearing value and its on and off,
    /// which setting the mode does, are not.
    /// </summary>
    [Fact]
    public void WhatADrillKeepsIsVettedAndTheGamesModeIsNot()
    {
        var ship = new Ship();
        var drill = ship.Rig.Entity(ship.Weld(new Bore(1, "Drill")).Id).Kind;

        Vetted(drill, ReachKind.Value, "drilling", "mode");
        Vetted(drill, ReachKind.Action, "TerrainClearingMode");
        Unvetted(drill, ReachKind.Value, "TerrainClearingMode");
        Unvetted(drill, ReachKind.Action, "TerrainClearingMode_On", "TerrainClearingMode_Off");
    }

    /// <summary>A projector's members are vetted, and the game's increase and decrease steps beside its placement are not.</summary>
    [Fact]
    public void WhatAProjectorKeepsIsVettedAndItsStepsAreNot()
    {
        var ship = new Ship();
        var projector = ship.Rig.Entity(ship.Weld(new Printer(1, "Printer")).Id).Kind;

        Vetted(projector, ReachKind.Native, "remove-the-blueprint");
        Vetted(projector, ReachKind.Value, "projecting", "blocks-left", "armor-blocks-left", "total-blocks", "blocks-ready-to-weld",
            "KeepProjection", "X", "Y", "Z", "RotX", "RotY", "RotZ", "Scale", "ShowOnlyBuildable", "MarkMissingBlocks",
            "MarkUnfinishedBlocks");
        Vetted(projector, ReachKind.Action, "KeepProjection");
        foreach (var placed in new[] { "X", "Y", "Z", "RotX", "RotY", "RotZ", "Scale" })
            Unvetted(projector, ReachKind.Action, "Increase" + placed, "Decrease" + placed);
    }

    /// <summary>
    /// A store's, a vending machine's and a contract block's switches are vetted in Sprocket's words, and the vending machine's
    /// turning through its items.
    /// </summary>
    [Fact]
    public void WhatTheTradingBlocksKeepIsVetted()
    {
        var ship = new Ship();
        var store = ship.Rig.Entity(ship.Weld(new Shop(1, "Store")).Id).Kind;
        Vetted(store, ReachKind.Value, "AnyoneCanUse", "Searchable");
        Vetted(store, ReachKind.Action, "AnyoneCanUse", "Searchable");

        var machine = new Stood<IMyVendingMachine>(2, "Machine", typeof(MyObjectBuilder_VendingMachine), "VendingMachine",
            "Vending Machine");
        machine.Offering("AnyoneCanUse", "Anyone Can Use On/Off").Offering("Searchable", "Searchable On/Off");
        machine.Carrying("AnyoneCanUse", "Anyone Can Use", ValueKind.Flag).Carrying("Searchable", "Searchable", ValueKind.Flag);
        var vending = ship.Rig.Entity(ship.Weld(machine).Id).Kind;
        Vetted(vending, ReachKind.Native, "show-next-item", "show-previous-item");
        Vetted(vending, ReachKind.Value, "AnyoneCanUse", "Searchable");
        Vetted(vending, ReachKind.Action, "AnyoneCanUse", "Searchable");

        var desk = new Plain(3, "Contracts", new MyObjectBuilderType(typeof(MyObjectBuilder_ContractBlock)), "ContractBlock",
            "Contracts");
        desk.Offering("AnyoneCanUse", "Anyone Can Use On/Off").Carrying("AnyoneCanUse", "Anyone Can Use", ValueKind.Flag);
        var contracts = ship.Rig.Entity(ship.Weld(desk).Id).Kind;
        Vetted(contracts, ReachKind.Value, "AnyoneCanUse");
        Vetted(contracts, ReachKind.Action, "AnyoneCanUse");
    }

    /// <summary>
    /// A collector's, a grinder's and a welder's conveyor switch is vetted in Sprocket's words, with a tool's run by a pilot, and an
    /// upgrade module's two counts.
    /// </summary>
    [Fact]
    public void WhatTheToolsAndTheModuleKeepIsVetted()
    {
        var ship = new Ship();
        var tools = new Fitting[]
        {
            new Stood<IMyCollector>(1, "Collector", typeof(MyObjectBuilder_Collector), "Collector", "Collector"),
            new Stood<IMyShipGrinder>(2, "Grinder", typeof(MyObjectBuilder_ShipGrinder), "LargeShipGrinder", "Grinder"),
            new Stood<IMyShipWelder>(3, "Welder", typeof(MyObjectBuilder_ShipWelder), "LargeShipWelder", "Welder")
        };

        foreach (var tool in tools)
        {
            tool.Offering("UseConveyor", "Use Conveyor System On/Off").Carrying("UseConveyor", "Use Conveyor System", ValueKind.Flag);
            var kind = ship.Rig.Entity(ship.Weld(tool).Id).Kind;
            Vetted(kind, ReachKind.Value, "UseConveyor");
            Vetted(kind, ReachKind.Action, "UseConveyor");
            if (!(tool.Block is IMyCollector))
                Vetted(kind, ReachKind.Value, "run-by-a-pilot");
        }

        var module = ship.Weld(new Stood<IMyUpgradeModule>(4, "Module", typeof(MyObjectBuilder_UpgradeModule), "LargeProductivityModule",
            "Productivity Module"));
        Vetted(ship.Rig.Entity(module.Id).Kind, ReachKind.Value, "connections", "upgrade-count");
    }

    /// <summary>An assembler's mode is set by a step, to making or taking apart, and to nothing else.</summary>
    [Fact]
    public void AStepSetsAnAssemblersMode()
    {
        var ship = new Ship();
        var assembler = ship.Weld(new Factory(1, "Assembler", false));

        Assert.Equal(SequenceProblem.None, ship.Set(assembler, "mode", Literal.Name("taking-apart")));
        Assert.Equal(Sandbox.ModAPI.Ingame.MyAssemblerMode.Disassembly, assembler.Mode);

        Assert.Equal(SequenceProblem.None, ship.Set(assembler, "mode", Literal.Name("assembling")));
        Assert.Equal(Sandbox.ModAPI.Ingame.MyAssemblerMode.Assembly, assembler.Mode);
    }

    /// <summary>A mode that is none of an assembler's is the step's fault, and the assembler is left as it was.</summary>
    [Fact]
    public void AnAssemblerModeOutsideItsChoicesIsAFault()
    {
        var ship = new Ship();
        var assembler = ship.Weld(new Factory(1, "Assembler", false));

        Assert.Equal(SequenceProblem.Mistyped, ship.Set(assembler, "mode", Literal.Name("melting")));
        Assert.Equal(Sandbox.ModAPI.Ingame.MyAssemblerMode.Assembly, assembler.Mode);
    }

    /// <summary>Whether an assembler repeats its queue is set by a step, on and off, and only to on or off.</summary>
    [Fact]
    public void AStepSetsWhetherAnAssemblerRepeats()
    {
        var ship = new Ship();
        var assembler = ship.Weld(new Factory(1, "Assembler", false));

        Assert.Equal(SequenceProblem.None, ship.Set(assembler, "repeating", Literal.Of(true)));
        Assert.True(assembler.Repeating);

        Assert.Equal(SequenceProblem.None, ship.Set(assembler, "repeating", Literal.Of(false)));
        Assert.False(assembler.Repeating);

        Assert.Equal(SequenceProblem.Mistyped, ship.Set(assembler, "repeating", Literal.Of(3L)));
        Assert.False(assembler.Repeating);
    }

    /// <summary>A drill's mode is set by a step, to mining or clearing terrain, and a mode that is neither is the step's fault.</summary>
    [Fact]
    public void AStepSetsADrillsMode()
    {
        var ship = new Ship();
        var drill = ship.Weld(new Bore(1, "Drill"));

        Assert.Equal(SequenceProblem.None, ship.Set(drill, "mode", Literal.Name("clearing-terrain")));
        Assert.True(drill.Clearing);

        Assert.Equal(SequenceProblem.None, ship.Set(drill, "mode", Literal.Name("mining")));
        Assert.False(drill.Clearing);

        Assert.Equal(SequenceProblem.Mistyped, ship.Set(drill, "mode", Literal.Name("digging")));
        Assert.False(drill.Clearing);
    }

    /// <summary>Asserts each of <paramref name="ids" /> is on <paramref name="kind" /> as <paramref name="form" />, and vetted.</summary>
    static void Vetted(EntityKind kind, ReachKind form, params string[] ids)
    {
        foreach (var id in ids)
        {
            var member = kind.FindAction(id, form);
            Assert.True(member != null, kind.Id + " has no " + form + " " + id);
            Assert.True(member.Vetted, kind.Id + " " + form + " " + id + " is not vetted");
        }
    }

    /// <summary>Asserts each of <paramref name="ids" /> is on <paramref name="kind" /> as <paramref name="form" />, and not vetted.</summary>
    static void Unvetted(EntityKind kind, ReachKind form, params string[] ids)
    {
        foreach (var id in ids)
        {
            var member = kind.FindAction(id, form);
            Assert.True(member != null, kind.Id + " has no " + form + " " + id);
            Assert.False(member.Vetted, kind.Id + " " + form + " " + id + " is vetted");
        }
    }
}
