using System.Xml.Linq;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Machine;
using Sprocket.Sim.Scenarios;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Each scenario runs in the harness, and its blueprint holds what a paste needs: every block placed as the game would, its program, and its buttons wired.</summary>
/// <remarks>The blocks come from the game's own definitions, so these need the game installed, or the harness's cache of its content.</remarks>
public class ScenarioTests
{
    static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    /// <summary>Every scenario there is, so each is checked the same way.</summary>
    public static IEnumerable<object[]> All => Scenarios.All.Select(scenario => new object[] { scenario.Name });

    /// <summary>Each of the split twin's buttons runs its own doors through a whole cycle, every time it is pressed.</summary>
    [Fact]
    public void TheSplitTwinsButtonsEachCycleTheirOwnDoorsEveryTime()
    {
        var built = new SplitTwin().Build(BlockDefinitions.FromGame());
        var sim = built.Sim;

        for (var press = 0; press < 2; press++)
        {
            foreach (var button in new[] { "Fore Button", "Aft Button" })
            {
                sim.Press(button);
                for (var i = 0; i < 600; i++)
                    sim.Tick();
            }
        }

        var log = sim.Machine.Log;
        foreach (var door in new[] { "Fore Inner Door", "Fore Outer Door", "Aft Inner Door", "Aft Outer Door" })
        {
            Assert.Equal(2, log.Count(line => line == door + ":open"));
            Assert.Equal(2, log.Count(line => line == door + ":close"));
        }
    }

    /// <summary>
    /// The split twin ground apart at its bridge leaves each half running its own airlock and only its own; welded back, it is
    /// one automation again with both, and every button works throughout.
    /// </summary>
    [Fact]
    public void TheSplitTwinSplitsAndJoinsAgain()
    {
        var scenario = new SplitTwin();
        var built = scenario.Build(BlockDefinitions.FromGame());
        var sim = built.Sim;
        var aftHero = built.Deck.Blocks.Single(fitting => fitting.Name == "Aft Automation");

        var aftPart = built.Deck.Parts(scenario.Bridge).Single(part => part.Contains(aftHero));
        var parted = sim.Machine.Split(aftPart);
        Settle(sim);

        Assert.Equal(2, sim.Roster.Count);
        var fore = sim.Roster.Kept(sim.Machine.Sprocket.Block)!;
        var aft = sim.Roster.Kept(aftHero.Block)!;
        Assert.Equal(new[] { "Fore" }, fore.Program.Systems[0].Instances.Select(instance => instance.Name));
        Assert.Equal(new[] { "Aft" }, aft.Program.Systems[0].Instances.Select(instance => instance.Name));
        Cycles(sim, "Fore Button", "Fore Inner Door");
        Cycles(sim, "Aft Button", "Aft Inner Door");

        sim.Machine.Merge(parted);
        Settle(sim);

        var joined = Assert.Single(sim.Roster.All);
        Assert.Equal(new[] { "Fore", "Aft" }, joined.Program.Systems.Single().Instances.Select(instance => instance.Name));
        Assert.Empty(joined.Memory.Conflicts);
        Cycles(sim, "Fore Button", "Fore Inner Door");
        Cycles(sim, "Aft Button", "Aft Inner Door");
    }

    /// <summary>Ticks long enough for the construct's change to be noticed and answered.</summary>
    static void Settle(Simulation sim)
    {
        for (var i = 0; i < 5; i++)
            sim.Tick();
    }

    /// <summary>Presses <paramref name="button" /> and waits out the cycle, asserting <paramref name="door" /> opened once more.</summary>
    static void Cycles(Simulation sim, string button, string door)
    {
        var before = sim.Machine.Log.Count(line => line == door + ":open");
        sim.Press(button);
        for (var i = 0; i < 600; i++)
            sim.Tick();

        Assert.True(sim.Machine.Log.Count(line => line == door + ":open") == before + 1, $"{button} did not cycle {door}");
    }

    /// <summary>A scenario's programmable block carries its program, whose every block is found again by the cell the blueprint puts it in.</summary>
    [Theory]
    [MemberData(nameof(All))]
    public void TheProgramFindsEveryBlockWhereTheBlueprintPutsIt(string name)
    {
        var scenario = Scenarios.Named(name);
        var built = scenario.Build(BlockDefinitions.FromGame());
        var blueprint = XDocument.Parse(scenario.Blueprint(built));

        var program = blueprint.Descendants("item").Single(item => (string)item.Element("Key") == SprocketUnit.ProgramKey.ToString("D"));
        var read = new Automation();
        Assert.Equal("", ProgramText.Load((string)program.Element("Value")!, read, built.Sim.World.Values));

        Assert.NotEmpty(read.Refs);
        foreach (var declared in read.Refs)
        {
            var block = built.Deck.Blocks.Single(fitting => declared.Keys.Any(key => key.Kind == ValueKind.Integer && key.Value.Bits == fitting.Id));
            var place = Assert.Single(declared.Keys, key => key.Cells != null).Cells!;
            Assert.Equal(built.Deck[block].Position(built.Deck.Defined(block)), place[place.Length - 1]);
        }
    }

    /// <summary>Every scenario stands a sign showing its checks, in lines a text panel shows whole, since it never wraps one.</summary>
    [Theory]
    [MemberData(nameof(All))]
    public void EveryScenarioShowsItsChecksOnASign(string name)
    {
        var scenario = Scenarios.Named(name);
        var built = scenario.Build(BlockDefinitions.FromGame());
        var blueprint = XDocument.Parse(scenario.Blueprint(built));

        var sign = Assert.Single(blueprint.Descendants("MyObjectBuilder_CubeBlock"), block => (string)block.Element("EntityId") == Scenario.SignId.ToString());
        var text = (string)sign.Descendants("Text").Single();
        Assert.StartsWith(scenario.Name.ToUpperInvariant(), text);
        Assert.Equal(scenario.Checks.Count, text.Split('\n').Count(line => line.StartsWith("Expect: ")));
        Assert.All(text.Split('\n'), line => Assert.True(line.Length <= Instructions.Width, $"'{line}' is too long for the sign"));
    }

    /// <summary>Every battery in a scenario's blueprint is charged full, so a pasted construct has power at once.</summary>
    [Theory]
    [MemberData(nameof(All))]
    public void EveryBatteryIsChargedFull(string name)
    {
        var scenario = Scenarios.Named(name);
        var built = scenario.Build(BlockDefinitions.FromGame());
        var blueprint = XDocument.Parse(scenario.Blueprint(built));

        var batteries = blueprint.Descendants("MyObjectBuilder_CubeBlock").Where(block => (string)block.Attribute(Xsi + "type") == "MyObjectBuilder_BatteryBlock").ToList();
        Assert.NotEmpty(batteries);
        foreach (var battery in batteries)
            Assert.Equal("3", (string)battery.Element("CurrentStoredPower"));
    }

    /// <summary>Every button a scenario's program is started from holds Sprocket's action on the block that keeps the automation, as the blueprint writes it.</summary>
    [Theory]
    [MemberData(nameof(All))]
    public void EveryButtonIsWiredInTheBlueprint(string name)
    {
        var scenario = Scenarios.Named(name);
        var built = scenario.Build(BlockDefinitions.FromGame());
        var blueprint = XDocument.Parse(scenario.Blueprint(built));

        var panels = blueprint.Descendants("MyObjectBuilder_CubeBlock").Where(block => (string)block.Attribute(Xsi + "type") == "MyObjectBuilder_ButtonPanel").ToList();
        Assert.Equal(built.Deck.Blocks.Count(fitting => fitting is Panel), panels.Count);
        foreach (var panel in panels)
        {
            var data = Assert.Single(panel.Element("Toolbar")!.Element("Slots")!.Elements("Slot")).Element("Data")!;
            Assert.Equal(SignalAction.ActionId, (string)data.Element("Action"));
            Assert.Equal(built.Sim.Unit.Block.EntityId, (long)data.Element("BlockEntityId")!);
        }
    }
}
