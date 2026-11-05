using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using VRage.Game;
using VRage.ObjectBuilders;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A value read off blocks reads as the member declares it: its noun, its details, the part of the blocks, its join, the blocks.</summary>
public class ValueSentenceTests
{
    [Fact]
    public void ABlockWithOneInventoryIsNotAskedWhichAndTheAmountIsInIt()
    {
        Assert.Equal("if the amount of Ingot/Iron in Cargo 1 is at least 100 kg", Amount(1L, true, false));
    }

    [Fact]
    public void AProductionBlockNamesItsInventoryOfTheBlock()
    {
        Assert.Equal("if the amount of Ingot/Iron in the input of Refinery 1 is at least 100 kg", Amount(1L, false, true));
        Assert.Equal("if the amount of Ingot/Iron in the output of Refinery 1 is at least 100 kg", Amount(2L, false, true));
    }

    [Fact]
    public void BlocksThatNameTheirPartsDifferentlyFallToNumbers()
    {
        Assert.Equal("if the sum of the amount of Ingot/Iron in inventory 2 of Cargo 1 and Refinery 1 is at least 100 kg",
            Amount(2L, true, true));
    }

    [Fact]
    public void AnInventoryABlockDoesNotHaveIsSaid()
    {
        Assert.Equal("if the amount of Ingot/Iron in inventory 2 of Cargo 1 is at least 100 kg", Amount(2L, true, false));
    }

    [Fact]
    public void AnItemCountedByThePieceSaysPcs()
    {
        Assert.Equal("if the amount of Component/SteelPlate in Cargo 1 is at least 100 pcs", Amount(1L, true, false, "Component/SteelPlate"));
        Assert.Equal("if the amount of Component/SteelPlate in Cargo 1 is at least 1 pc", Amount(1L, true, false, "Component/SteelPlate", 1L));
    }

    [Fact]
    public void TheItemsOfferedAreTheOnesTheBlockCanHold()
    {
        var rig = Rig.Demo();
        var refinery = rig.Refinery(40, "Refinery 1");
        refinery.Accepts(new MyDefinitionId(new MyObjectBuilderType(typeof(MyObjectBuilder_Ore)), "Iron"));
        refinery.Accepts(new MyDefinitionId(new MyObjectBuilderType(typeof(MyObjectBuilder_Ingot)), "Iron"));
        var sim = Simulation.On(rig);

        var held = ItemIds.Held(sim.World.FindEntity(refinery.Id).Block);
        Assert.Equal(new[] { "Ingot/Iron", "Ore/Iron" }, held.OrderBy(item => item).ToArray());
    }

    [Fact]
    public void ALabelIsOfAButtonOnThePanel()
    {
        var rig = Rig.Demo();
        var panel = rig.Panel(40, "Button Panel 1", 4);
        var test = new Comparison
        {
            MemberId = "label-of-button", Provider = BlockKinds.Own, Quantifier = Quantifier.Any, Relation = Relation.Is,
            Value = Literal.Of("Exit")
        };
        test.Arguments.Add(Literal.Of(2L));
        Assert.Equal("if the label of button 2 on Button Panel 1 is Exit", Reads(rig, test, panel));
    }

    [Fact]
    public void AnOnlyButtonIsNotNamed()
    {
        var rig = Rig.Demo();
        var panel = rig.Panel(40, "Button Panel 1");
        var test = new Comparison
        {
            MemberId = "label-of-button", Provider = BlockKinds.Own, Quantifier = Quantifier.Any, Relation = Relation.Is,
            Value = Literal.Of("Exit")
        };
        test.Arguments.Add(Literal.Of(1L));
        Assert.Equal("if the label on Button Panel 1 is Exit", Reads(rig, test, panel));
    }

    static string Amount(long inventory, bool cargo, bool refinery, string item = "Ingot/Iron", long amount = 100L)
    {
        var rig = Rig.Demo();
        var blocks = new List<Fitting>();
        var holds = rig.Cargo(40, "Cargo 1");
        var turns = rig.Refinery(41, "Refinery 1");
        if (cargo)
            blocks.Add(holds);

        if (refinery)
            blocks.Add(turns);

        var test = new Comparison
        {
            MemberId = "inventory_amount", Provider = BlockKinds.Own, Quantifier = Quantifier.Sum, Relation = Relation.AtLeast,
            Value = Literal.Of(amount)
        };
        test.Arguments.Add(Literal.Of(inventory));
        test.Arguments.Add(Literal.Of(item));
        return Reads(rig, test, blocks.ToArray());
    }

    static string Reads(Rig rig, Comparison test, params Fitting[] blocks)
    {
        var sim = Simulation.On(rig);
        sim.Editor.NewButtonSystem("Stores");
        sim.Tick();

        var sequence = sim.Editor.Program.Systems[0].Sequences[0];
        foreach (var block in blocks)
            test.Targets.Add(sim.Editor.Program.Declare(sim.World, sim.World.FindEntity(block.Id)).Name);

        var step = new If(test);
        sequence.Steps.Add(step);
        return SequenceText.Step(sim.Editor.Runner.Bindings, sequence, step, sim.Editor.StandIn(sequence.System));
    }
}
