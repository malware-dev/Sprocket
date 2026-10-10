using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Blocks;
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
using MyInventoryItemFilter = Sandbox.ModAPI.Ingame.MyInventoryItemFilter;

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
                    .Gives("Mode", () => Sandbox.ModAPI.Ingame.MyAssemblerMode.Assembly)
                    .Gives("Repeating", () => false)
                    .Does("add_CurrentProgressChanged", args => _assembling.Add((Action<IMyAssembler>)args[0]))
                    .Does("remove_CurrentProgressChanged", args => _assembling.Remove((Action<IMyAssembler>)args[0]))
                    .Does("add_CurrentStateChanged", args => _assembling.Add((Action<IMyAssembler>)args[0]))
                    .Does("remove_CurrentStateChanged", args => _assembling.Remove((Action<IMyAssembler>)args[0]))
                    .Does("add_CurrentModeChanged", args => _assembling.Add((Action<IMyAssembler>)args[0]))
                    .Does("remove_CurrentModeChanged", args => _assembling.Remove((Action<IMyAssembler>)args[0])));

            Offering("UseConveyor", "Use Conveyor System On/Off");
            Carrying("UseConveyor", "Use Conveyor System", ValueKind.Flag);
        }

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

    /// <summary>A conveyor sorter that writes down what is added to its list and lets through what a test says.</summary>
    sealed class Gate : Fitting
    {
        public Gate(long id, string name)
            : base(id, name, new MyObjectBuilderType(typeof(MyObjectBuilder_ConveyorSorter)), "LargeBlockConveyorSorter", "Conveyor Sorter")
        {
            Fit(Pretend.To<IMyConveyorSorter>()
                .Gives("Mode", () => Mode)
                .Does("AddItem", args => Added.Add((MyInventoryItemFilter)args[0]))
                .Does("RemoveItem", args => Removed.Add((MyInventoryItemFilter)args[0]))
                .Does("SetFilter", args =>
                {
                    Mode = (Sandbox.ModAPI.Ingame.MyConveyorSorterMode)args[0];
                    Cleared = ((List<MyInventoryItemFilter>)args[1]).Count == 0;
                })
                .Does("IsAllowed", args => ((MyDefinitionId)args[0]).SubtypeName == Allows));
        }

        public List<MyInventoryItemFilter> Added { get; } = new();

        public List<MyInventoryItemFilter> Removed { get; } = new();

        public Sandbox.ModAPI.Ingame.MyConveyorSorterMode Mode { get; set; } = Sandbox.ModAPI.Ingame.MyConveyorSorterMode.Whitelist;

        public bool Cleared { get; private set; }

        public string Allows { get; set; } = "";
    }

    /// <summary>A drill whose mode a test sets.</summary>
    sealed class Bore : Fitting
    {
        public Bore(long id, string name)
            : base(id, name, new MyObjectBuilderType(typeof(MyObjectBuilder_Drill)), "LargeBlockDrill", "Drill")
        {
            Fit(Pretend.To<IMyShipDrill>()
                .Gives("TerrainClearingMode", () => Clearing)
                .Gives("IsActivated", () => Enabled));
        }

        public bool Clearing { get; set; }
    }

    /// <summary>A store that offers the game's own access switch.</summary>
    sealed class Shop : Fitting
    {
        public Shop(long id, string name)
            : base(id, name, new MyObjectBuilderType(typeof(MyObjectBuilder_StoreBlock)), "StoreBlock", "Store")
        {
            Fit(Pretend.To<IMyStoreBlock>());
            Offering("AnyoneCanUse", "Anyone Can Use On/Off");
            Carrying("AnyoneCanUse", "Anyone Can Use", ValueKind.Flag);
        }
    }

    /// <summary>A projector whose blocks left a test sets.</summary>
    sealed class Printer : Fitting
    {
        public Printer(long id, string name)
            : base(id, name, new MyObjectBuilderType(typeof(MyObjectBuilder_Projector)), "LargeProjector", "Projector")
        {
            Fit(Pretend.To<IMyProjector>().Gives("RemainingBlocks", () => Left));
        }

        public int Left { get; set; }
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
            var off = new Act(BlockKinds.SwitchOff) { Form = Form.Native };
            off.Targets.Add(Name(Lamp));
            trigger.Does = off;
            Program.AddSequence((Trigger)null!).System.AddTrigger(trigger);
        }

        public int Fired => Rig.Rig.Log.Count(line => line == "Lamp:off");
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

        Assert.NotNull(kind.FindAction("make", Form.Native));
        Assert.NotNull(kind.FindAction("clear-the-queue", Form.Native));
        foreach (var value in new[]
                 {
                     "items-queued", "progress", "producing", "stuck", "mode", "able-to-take-apart", "repeating", "input-fill",
                     "output-fill", "amount-in-input", "amount-in-output"
                 })
        {
            var own = kind.FindAction(value, Form.Value);
            Assert.True(own?.Being != null, value);
            Assert.True(own.Announced || own.LookedBecause.Length > 0, value);
        }

        Assert.True(kind.FindEvent("starts-producing")!.Announced);
        Assert.True(kind.FindEvent("stops-producing")!.Announced);
        Assert.Null(kind.FindAction("refining", Form.Value));
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
                if (value.Being != null && !value.Announced && value.RuledOutBy == null && value.LookedBecause.Length == 0)
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

        Assert.NotNull(kind.FindAction("refining", Form.Value));
        Assert.NotNull(kind.FindEvent("starts-refining"));
        Assert.NotNull(kind.FindEvent("stops-refining"));
        Assert.Null(kind.FindAction("producing", Form.Value));
        Assert.Null(kind.FindAction("make", Form.Native));
        Assert.Null(kind.FindAction("items-queued", Form.Value));
        Assert.NotNull(kind.FindAction("input-fill", Form.Value));
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
        var toggle = assembler.FindAction("UseConveyor", Form.Action)!;
        Assert.False(toggle.Hidden);
        Assert.Equal("toggle automatic push and pull", toggle.Text);
        Assert.Equal("Automatic push and pull", assembler.FindAction("UseConveyor", Form.Value)!.Named);

        var generator = new Plain(2, "Generator", new MyObjectBuilderType(typeof(MyObjectBuilder_OxygenGenerator)),
            "OxygenGenerator", "O2/H2 Generator");
        generator.Offering("UseConveyor", "Use Conveyor System On/Off");
        ship.Weld(generator);
        Assert.True(ship.Rig.Entity(generator.Id).Kind.FindAction("UseConveyor", Form.Action)!.Hidden);
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

        var value = ship.Rig.Entity(assembler.Id).Kind.FindAction("stuck", Form.Value)!;
        Assert.Equal(Literal.Of(stuck), value.Being.Gauge(assembler.Block, new Literal[0], null).Read());
    }

    /// <summary>How full an inventory is reads as a percentage, as fill does everywhere in the game's terminal.</summary>
    [Fact]
    public void FillReadsAsAPercentage()
    {
        var ship = new Ship();
        var assembler = ship.Weld(new Factory(1, "Assembler", false));
        assembler.InputFill = 0.4f;

        var fill = ship.Rig.Entity(assembler.Id).Kind.FindAction("input-fill", Form.Value)!;
        Assert.Equal(40.0, fill.Being.Gauge(assembler.Block, new Literal[0], null).Read().Real, 3);
    }

    /// <summary>How much of an item an inventory holds is a function of the item, heard as the inventory changes.</summary>
    [Fact]
    public void TheAmountOfAnItemIsHeardAsTheInventoryChanges()
    {
        var ship = new Ship();
        var assembler = ship.Weld(new Factory(1, "Assembler", false));

        var when = new WhenTrigger("amount-in-input")
        {
            Provider = BlockKinds.Own, Quantifier = Quantifier.Any, Relation = Relation.AtLeast, Value = Literal.Of(100L)
        };
        when.Arguments.Add(Literal.Of("Ingot/Iron"));
        when.Sources.Add(ship.Name(assembler));
        ship.Switching(when);
        ship.Start();
        ship.Tick(20);
        Assert.Equal(0, ship.Fired);

        assembler.Iron = 250;
        assembler.Changed();
        ship.Tick();

        Assert.Equal(1, ship.Fired);
    }

    /// <summary>An item is written as its type and subtype, and one the game has no type for names nothing.</summary>
    [Theory]
    [InlineData("Ingot/Iron", true)]
    [InlineData("Ore/Ice", true)]
    [InlineData("Ingot", false)]
    [InlineData("Ingot/", false)]
    [InlineData("Nonsense/Iron", false)]
    public void AnItemIsItsTypeAndSubtype(string said, bool known)
    {
        Assert.Equal(known, Items.Read(said, out _));
    }

    /// <summary>A sorter's list takes a whole kind of item, or one item, and nothing it cannot name.</summary>
    [Fact]
    public void ASorterListTakesAKindOrAnItem()
    {
        var ship = new Ship();
        var sorter = ship.Weld(new Gate(1, "Sorter"));
        var entity = ship.Rig.Entity(sorter.Id);

        entity.Invoke("add-to-the-list", new[] { Literal.Of("Ore") });
        entity.Invoke("add-to-the-list", new[] { Literal.Of("Ingot/Iron") });
        entity.Invoke("add-to-the-list", new[] { Literal.Of("Nonsense") });

        Assert.Equal(2, sorter.Added.Count);
        Assert.True(sorter.Added[0].AllSubTypes);
        Assert.Equal("MyObjectBuilder_Ore", sorter.Added[0].ItemId.TypeId.ToString());
        Assert.False(sorter.Added[1].AllSubTypes);
        Assert.Equal("Iron", sorter.Added[1].ItemId.SubtypeName);
    }

    /// <summary>An item comes off a sorter's list as it went on, and clearing the list keeps whether it lets the list through or holds it back.</summary>
    [Fact]
    public void ASorterListIsTakenFromAndCleared()
    {
        var ship = new Ship();
        var sorter = ship.Weld(new Gate(1, "Sorter"));
        sorter.Mode = Sandbox.ModAPI.Ingame.MyConveyorSorterMode.Blacklist;
        var entity = ship.Rig.Entity(sorter.Id);

        entity.Invoke("remove-from-the-list", new[] { Literal.Of("Ore/Ice") });
        entity.Invoke("clear-the-list", new Literal[0]);

        Assert.Equal("Ice", Assert.Single(sorter.Removed).ItemId.SubtypeName);
        Assert.True(sorter.Cleared);
        Assert.Equal(Sandbox.ModAPI.Ingame.MyConveyorSorterMode.Blacklist, sorter.Mode);
    }

    /// <summary>A drill's mode is one of mining and clearing terrain, read off the game's own switch for it.</summary>
    [Fact]
    public void ADrillsModeIsMiningOrClearingTerrain()
    {
        var ship = new Ship();
        var drill = ship.Weld(new Bore(1, "Drill"));
        var mode = ship.Rig.Entity(drill.Id).Kind.FindAction("mode", Form.Value)!;

        Assert.Equal(new[] { "mining", "clearing-terrain" }, mode.Being.Names);
        Assert.Equal(Literal.Of(0L), mode.Being.Gauge(drill.Block, new Literal[0], null).Read());
        drill.Clearing = true;
        Assert.Equal(Literal.Of(1L), mode.Being.Gauge(drill.Block, new Literal[0], null).Read());
    }

    /// <summary>Who may use a store is offered with its toggle, in Sprocket's words; another block's toggle stays out of the list.</summary>
    [Fact]
    public void TheAccessToggleIsOfferedOnAStore()
    {
        var ship = new Ship();
        var store = ship.Rig.Entity(ship.Weld(new Shop(1, "Store")).Id).Kind;
        var toggle = store.FindAction("AnyoneCanUse", Form.Action)!;
        Assert.False(toggle.Hidden);
        Assert.Equal("toggle anyone can use", toggle.Text);

        var panel = new Plain(2, "Panel", new MyObjectBuilderType(typeof(MyObjectBuilder_ButtonPanel)), "ButtonPanelLarge",
            "Button Panel");
        panel.Offering("AnyoneCanUse", "Anyone Can Use On/Off");
        ship.Weld(panel);
        Assert.True(ship.Rig.Entity(panel.Id).Kind.FindAction("AnyoneCanUse", Form.Action)!.Hidden);
    }

    /// <summary>Whether a sorter lets an item through is a flag given the item.</summary>
    [Fact]
    public void ASorterSaysWhetherItLetsAnItemThrough()
    {
        var ship = new Ship();
        var sorter = ship.Weld(new Gate(1, "Sorter"));
        sorter.Allows = "Ice";
        var lets = ship.Rig.Entity(sorter.Id).Kind.FindAction("lets-through", Form.Value)!;

        Assert.Equal(ValueKind.Flag, lets.Being.Kind);
        Assert.Equal(Literal.Of(true), lets.Being.Gauge(sorter.Block, new[] { Literal.Of("Ore/Ice") }, new Recall()).Read());
        Assert.Equal(Literal.Of(false), lets.Being.Gauge(sorter.Block, new[] { Literal.Of("Ore/Iron") }, new Recall()).Read());
        Assert.Equal(Literal.Of(false), lets.Being.Gauge(sorter.Block, new[] { Literal.Of("Nonsense/Ice") }, new Recall()).Read());
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
        var item = Items.Choosing("item", new[] { "Iron Ingot" }, new[] { "Ingot/Iron" });

        var function = Being.Flag<IMyTerminalBlock, Item>(Item.First, (_, _, _) => true).Taking(item);
        Assert.Null(function.Refused(new[] { Literal.Of("Ingot/Iron") }));
        Assert.Equal("item", function.Refused(new[] { Literal.Of("Ingot/Gold") }));
    }

    /// <summary>Where no item is listed, as where the game has loaded none, an item is any written out.</summary>
    [Fact]
    public void AnItemWithNothingListedIsAnyWrittenOut()
    {
        var item = Items.Choosing("item", new string[0], new string[0]);
        var function = Being.Flag<IMyTerminalBlock, Item>(Item.First, (_, _, _) => true).Taking(item);

        Assert.Null(function.Refused(new[] { Literal.Of("Ingot/Gold") }));
        Assert.Equal("item", function.Refused(new[] { Literal.Of(3L) }));
    }
}
