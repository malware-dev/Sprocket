using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Sim.Machine;
using VRage.Game;
using VRage.ObjectBuilders;
using Xunit;

namespace Sprocket.Tests;

/// <summary>Moving items from one inventory to another, a step that takes the slots they go to as an argument.</summary>
public class MoveItemsTests
{
    /// <summary>Registers the game's kinds of item by name, as the game does as it starts, so an item can be read.</summary>
    public MoveItemsTests()
    {
        MyObjectBuilderType.RegisterFromAssembly(typeof(MyObjectBuilder_Ingot).Assembly);
    }

    /// <summary>A logic system with the slots from and to, filled with the given inventories, and one sequence moving items between them.</summary>
    static (Automation Program, AutomationRunner Runner, Sequence Sequence, SystemInstance Installation) Moving(Rigged ship,
        (Stockroom Block, int Facet) from, (Stockroom Block, int Facet) to, params Literal[] options)
    {
        var program = new Automation();
        var system = program.AddSystem();
        system.Name = "Moves";
        system.IsBlueprint = true;
        var installation = system.AddInstance();
        system.PartFor("from").Holds = new InventoryType().Id;
        system.PartFor("to").Holds = new InventoryType().Id;
        installation.Fill("from", new[] { program.Declare(ship.World, ship.Entity(from.Block.Id)).Name }, new[] { from.Facet });
        installation.Fill("to", new[] { program.Declare(ship.World, ship.Entity(to.Block.Id)).Name }, new[] { to.Facet });

        var sequence = program.AddSequence(system, null!);
        var act = new Act(InventoryType.MoveItems) { Form = ReachKind.Native, Provider = BlockKinds.Own };
        act.Targets.Add("from");
        act.Arguments.Add(Literal.Slots(new List<string> { "to" }));
        foreach (var option in options)
            act.Arguments.Add(option);

        sequence.Steps.Add(act);
        sequence.Steps.Add(new Await());
        return (program, new AutomationRunner(program, ship.World), sequence, installation);
    }

    static void Run(Rigged ship, AutomationRunner runner, Sequence sequence, SystemInstance installation, int frames = 40)
    {
        ship.Tick();
        runner.Tick(1);
        runner.Call(sequence, installation);
        Carry(ship, runner, 2, frames);
    }

    static void Carry(Rigged ship, AutomationRunner runner, int from, int frames)
    {
        for (var frame = from; frame < from + frames; frame++)
        {
            ship.Tick();
            runner.Tick(frame);
        }
    }

    /// <summary>A set amount of an item leaves one container for the other.</summary>
    [Fact]
    public void ASetAmountMoves()
    {
        var ship = new Rigged();
        var source = ship.Rig.Cargo(1, "Source");
        var target = ship.Rig.Cargo(2, "Target");
        source.Holds("Iron", 250);
        var (_, runner, sequence, installation) = Moving(ship, (source, 1), (target, 1), Literal.Of("Ingot/Iron"), Literal.Of(100L));

        Run(ship, runner, sequence, installation);

        Assert.Equal(150, source.Holding("Iron"), 3);
        Assert.Equal(100, target.Holding("Iron"), 3);
        Assert.False(runner.Get(sequence, installation).IsRunning);
    }

    /// <summary>With no amount, everything of the item moves.</summary>
    [Fact]
    public void WithNoAmountAllOfItMoves()
    {
        var ship = new Rigged();
        var source = ship.Rig.Cargo(1, "Source");
        var target = ship.Rig.Cargo(2, "Target");
        source.Holds("Iron", 250);
        source.Holds("Nickel", 30);
        var (_, runner, sequence, installation) = Moving(ship, (source, 1), (target, 1), Literal.Of("Ingot/Iron"));

        Run(ship, runner, sequence, installation);

        Assert.Equal(0, source.Holding("Iron"), 3);
        Assert.Equal(30, source.Holding("Nickel"), 3);
        Assert.Equal(250, target.Holding("Iron"), 3);
    }

    /// <summary>Two inventories of one block are two things: a refinery's output moves into its own input.</summary>
    [Fact]
    public void OneBlockMovesBetweenItsOwnInventories()
    {
        var ship = new Rigged();
        var refinery = ship.Rig.Refinery(1, "Refinery");
        refinery.Holds("Iron", 40, 1);
        var (_, runner, sequence, installation) = Moving(ship, (refinery, 2), (refinery, 1), Literal.Of("Ingot/Iron"));

        Run(ship, runner, sequence, installation);

        Assert.Equal(0, refinery.Holding("Iron", 1), 3);
        Assert.Equal(40, refinery.Holding("Iron"), 3);
    }

    /// <summary>A step that holds waits for room, moves what fits meanwhile, and finishes once the rest fits.</summary>
    [Fact]
    public void HoldingWaitsForRoom()
    {
        var ship = new Rigged();
        var source = ship.Rig.Cargo(1, "Source");
        var target = ship.Rig.Cargo(2, "Target");
        source.Holds("Iron", 100);
        target.Room(30);
        var (_, runner, sequence, installation) = Moving(ship, (source, 1), (target, 1), Literal.Of("Ingot/Iron"), Literal.Of(100L));

        Run(ship, runner, sequence, installation);

        Assert.Equal(30, target.Holding("Iron"), 3);
        Assert.True(runner.Get(sequence, installation).IsRunning);

        target.Room(1000);
        Carry(ship, runner, 42, 400);

        Assert.Equal(100, target.Holding("Iron"), 3);
        Assert.False(runner.Get(sequence, installation).IsRunning);
    }

    /// <summary>A step told to move on moves what fits and carries on.</summary>
    [Fact]
    public void MovingOnCarriesOnWithWhatFit()
    {
        var ship = new Rigged();
        var source = ship.Rig.Cargo(1, "Source");
        var target = ship.Rig.Cargo(2, "Target");
        source.Holds("Iron", 100);
        target.Room(30);
        var (_, runner, sequence, installation) = Moving(ship, (source, 1), (target, 1), Literal.Of("Ingot/Iron"), Literal.Of(100L),
            Literal.Of(InventoryType.MoveOn));

        Run(ship, runner, sequence, installation, 400);

        Assert.Equal(30, target.Holding("Iron"), 3);
        Assert.False(runner.Get(sequence, installation).IsRunning);
    }

    /// <summary>Nothing moves over no conveyor: a step that holds stays stuck, says no conveyor connects them, and nothing leaves the source.</summary>
    [Fact]
    public void NoConveyorMovesNothing()
    {
        var ship = new Rigged();
        var source = ship.Rig.Cargo(1, "Source");
        var target = ship.Rig.Cargo(2, "Target");
        source.Holds("Iron", 100);
        source.Connected = false;
        var (_, runner, sequence, installation) = Moving(ship, (source, 1), (target, 1), Literal.Of("Ingot/Iron"));

        Run(ship, runner, sequence, installation, 400);

        Assert.Equal(100, source.Holding("Iron"), 3);
        Assert.Equal(0, target.Holding("Iron"), 3);
        Assert.True(runner.Get(sequence, installation).IsRunning);
        Assert.Equal(Cause.ItemsNotConnected, SequenceSummary.Fault(runner.Get(sequence, installation)!).Because.Cause);
    }

    /// <summary>A block refusing the item takes none of it, and a step that holds stays stuck, as a refinery's input refuses ingots.</summary>
    [Fact]
    public void ARefusedItemStaysWhereItIs()
    {
        var ship = new Rigged();
        var refinery = ship.Rig.Refinery(1, "Refinery");
        refinery.Accepts(new MyDefinitionId(new MyObjectBuilderType(typeof(MyObjectBuilder_Ore)), "Iron"));
        refinery.Holds("Iron", 40, 1);
        var (_, runner, sequence, installation) = Moving(ship, (refinery, 2), (refinery, 1), Literal.Of("Ingot/Iron"));

        Run(ship, runner, sequence, installation, 400);

        Assert.Equal(40, refinery.Holding("Iron", 1), 3);
        Assert.True(runner.Get(sequence, installation).IsRunning);
    }

    /// <summary>A slot list as an argument reads and writes back as it was.</summary>
    [Fact]
    public void ASlotListArgumentReadsAndWritesBack()
    {
        var ship = new Rigged();
        var source = ship.Rig.Cargo(1, "Source");
        var target = ship.Rig.Cargo(2, "Target");
        var (program, _, _, _) = Moving(ship, (source, 1), (target, 1), Literal.Of("Ingot/Iron"), Literal.Of(100L));
        var values = new Values();

        var text = ProgramWriter.Save(program, values);
        Assert.Contains("ext.move_items([ 'to' ], \"Ingot/Iron\", 100) on [ 'from' ]", text);

        var read = new Automation();
        Assert.Equal("", ProgramReader.Load(text, read, values));
        Assert.Equal(new[] { "to" }, ((Act)read.Systems[0].Sequences[0].Steps[0]).Arguments[0].Names);
        Assert.Equal(text, ProgramWriter.Save(read, values));
    }

    /// <summary>Renaming a slot renames it in an argument too, and a slot named only in an argument counts as used.</summary>
    [Fact]
    public void AnArgumentsSlotIsAUseOfTheSlot()
    {
        var ship = new Rigged();
        var source = ship.Rig.Cargo(1, "Source");
        var target = ship.Rig.Cargo(2, "Target");
        var (program, _, sequence, _) = Moving(ship, (source, 1), (target, 1));
        var system = program.Systems[0];

        Assert.Equal(1, system.Mentions("to"));

        system.RenameMentions("to", "bin");

        Assert.Equal(new[] { "bin" }, ((Act)sequence.Steps[0]).Arguments[0].Names);
    }

    /// <summary>The list of items opens on all items, which a step holds as no item given.</summary>
    [Fact]
    public void TheListOpensOnAllItems()
    {
        var field = InventoryType.OrAllItems(ItemIds.Choosing("item", new[] { "Iron Ingot" }, new[] { "Ingot/Iron" }));

        Assert.Equal(InventoryType.AllItems, field.Words[0]);
        Assert.Equal(InventoryType.AllItems, field.Default);
        Assert.Equal("", field.Meaning(InventoryType.AllItems));
        Assert.Equal("Ingot/Iron", field.Meaning("Iron Ingot"));
    }

    /// <summary>A line says all items rather than the word the list opens on.</summary>
    [Fact]
    public void ASentenceSaysItsOwnWordForNothingGiven()
    {
        var field = InventoryType.OrAllItems(ItemIds.Choosing("item", new[] { "Iron Ingot" }, new[] { "Ingot/Iron" }));
        field.SaidEmpty = "items";
        var slot = new Sprocket.Host.SpaceEngineers.Framework.Ui.Parts.ArgumentsSlot(new[] { Literal.Of("") }, new[] { field }, null!);

        Assert.Equal("items", slot.Filled[0]);
    }

    /// <summary>A list of items is searched however few it holds.</summary>
    [Fact]
    public void AListOfItemsIsAlwaysSearched()
    {
        Assert.True(ItemIds.Choosing("item", new[] { "Iron Ingot" }, new[] { "Ingot/Iron" }).Searched);
    }

    /// <summary>A slot holding an inventory reads how much of an item that inventory holds, whichever of the block's it is.</summary>
    [Fact]
    public void AnInventorySlotReadsItsOwnInventory()
    {
        var ship = new Rigged();
        var refinery = ship.Rig.Refinery(1, "Refinery");
        refinery.Holds("Iron", 40, 1);
        var entity = ship.Entity(refinery.Id);

        var amount = entity.Kind.FindAction("amount_held", ReachKind.Value)!;
        var first = new[] { Literal.Of(1L), Literal.Of("Ingot/Iron") };
        var second = new[] { Literal.Of(2L), Literal.Of("Ingot/Iron") };

        Assert.Equal(0.0, amount.OwnValue.Gauge(entity.Block, first, null).Read().Real, 3);
        Assert.Equal(40.0, amount.OwnValue.Gauge(entity.Block, second, null).Read().Real, 3);
        Assert.True(amount.InSlotsOnly);
    }

    /// <summary>A slot holding an inventory reads how full it is and how much its items weigh.</summary>
    [Fact]
    public void AnInventorySlotReadsItsFillAndMass()
    {
        var ship = new Rigged();
        var cargo = ship.Rig.Cargo(1, "Cargo");
        cargo.Holds("Iron", 40);
        cargo.FillFactor = 0.25f;
        var entity = ship.Entity(cargo.Id);
        var first = new[] { Literal.Of(1L) };

        Assert.Equal(25.0, entity.Kind.FindAction("fill_level", ReachKind.Value)!.OwnValue.Gauge(entity.Block, first, null).Read().Real, 3);
        Assert.Equal(40.0, entity.Kind.FindAction("mass_held", ReachKind.Value)!.OwnValue.Gauge(entity.Block, first, null).Read().Real, 3);
    }

    /// <summary>The cause a step waiting to move iron from the source to the target gives.</summary>
    static Cause StuckBecause(Rigged ship, Stockroom source, Stockroom target, int targetFacet = 1)
    {
        var (_, runner, sequence, installation) = Moving(ship, (source, 1), (target, targetFacet), Literal.Of("Ingot/Iron"), Literal.Of(100L));
        Run(ship, runner, sequence, installation);
        Assert.True(runner.Get(sequence, installation).IsRunning);
        return SequenceSummary.Fault(runner.Get(sequence, installation)!).Because.Cause;
    }

    /// <summary>With none of the item, the step says there is none to move.</summary>
    [Fact]
    public void WaitingWithNoStockSaysSo()
    {
        var ship = new Rigged();

        Assert.Equal(Cause.ItemsNoStock, StuckBecause(ship, ship.Rig.Cargo(1, "Source"), ship.Rig.Cargo(2, "Target")));
    }

    /// <summary>With nothing connecting them, the step says no conveyor does.</summary>
    [Fact]
    public void WaitingWithNoConnectionSaysSo()
    {
        var ship = new Rigged();
        var source = ship.Rig.Cargo(1, "Source");
        source.Holds("Iron", 100);
        source.Connected = false;

        Assert.Equal(Cause.ItemsNotConnected, StuckBecause(ship, source, ship.Rig.Cargo(2, "Target")));
    }

    /// <summary>With a target that takes the item and has no room for it, the step says there is no room.</summary>
    [Fact]
    public void WaitingForRoomSaysSo()
    {
        var ship = new Rigged();
        var source = ship.Rig.Cargo(1, "Source");
        var target = ship.Rig.Cargo(2, "Target");
        source.Holds("Iron", 100);
        target.Room(0);
        target.Accepts(MyDefinitionId.Parse("MyObjectBuilder_Ingot/Iron"));

        Assert.Equal(Cause.ItemsNoRoom, StuckBecause(ship, source, target));
    }

    /// <summary>With a target that takes none of the item, the step says it is refused.</summary>
    [Fact]
    public void WaitingOnARefusalSaysSo()
    {
        var ship = new Rigged();
        var source = ship.Rig.Cargo(1, "Source");
        var target = ship.Rig.Cargo(2, "Target");
        source.Holds("Iron", 100);
        target.Room(0);

        Assert.Equal(Cause.ItemsNotAccepted, StuckBecause(ship, source, target));
    }

    /// <summary>Sent to the inventory they are in, the items stay, and the step says they are already there.</summary>
    [Fact]
    public void MovingToTheSameInventorySaysTheyAreThere()
    {
        var ship = new Rigged();
        var source = ship.Rig.Cargo(1, "Source");
        source.Holds("Iron", 100);

        Assert.Equal(Cause.ItemsAlreadyThere, StuckBecause(ship, source, source));
        Assert.Equal(100, source.Holding("Iron"), 3);
    }

    /// <summary>A copy of a step has its own list of slots.</summary>
    [Fact]
    public void ACopyHasItsOwnList()
    {
        var names = new List<string> { "to" };
        var copied = Literal.Slots(names).Copied;

        names[0] = "bin";

        Assert.Equal(new[] { "to" }, copied.Names);
    }
}
