using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System.Xml.Linq;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Machine;
using Sprocket.Sim.Scenarios;
using Xunit;
using LandingGearMode = SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode;
using MyShipConnectorStatus = Sandbox.ModAPI.Ingame.MyShipConnectorStatus;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Each scenario runs in the harness, and its blueprint holds what a paste needs: every block placed as the game would, its program, and its buttons wired.</summary>
/// <remarks>The blocks come from the game's own definitions, so these need the game installed, or the harness's cache of its content.</remarks>
public class ScenarioTests
{
    static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    /// <summary>Every scenario there is, so each is checked the same way.</summary>
    public static IEnumerable<object[]> All => Scenarios.All.Select(scenario => new object[] { scenario.Name });

    /// <summary>Stock passing its level in the cargo container and in the refinery's input flashes the light, every time it is put in.</summary>
    [Fact]
    public void TheStockWatchFlashesEachTimeStockArrives()
    {
        VRage.ObjectBuilders.MyObjectBuilderType.RegisterFromAssembly(typeof(VRage.Game.MyObjectBuilder_Ingot).Assembly);
        var scenario = new StockWatch();
        var sim = scenario.Build(BlockDefinitions.FromGame()).Sim;

        for (var round = 0; round < 2; round++)
        {
            scenario.Cargo.Holds("Iron", 200);
            Run(sim);
            scenario.Cargo.Holds("Iron", 0);
            Run(sim);
            scenario.Refinery.Holds("Iron", 200);
            Run(sim);
            scenario.Refinery.Holds("Iron", 0);
            Run(sim);
        }

        Assert.Equal(4, sim.Machine.Log.Count(line => line == "Stock Light:on"));
        Assert.Equal(4, sim.Machine.Log.Count(line => line == "Stock Light:off"));
    }

    /// <summary>Three Button moves the piston to 3 m and switches At Three on once it is there, every time it is pressed.</summary>
    [Fact]
    public void ThePistonStatusMovesToThreeEachPress()
    {
        var scenario = new PistonStatus();
        var sim = scenario.Build(BlockDefinitions.FromGame()).Sim;

        for (var round = 0; round < 2; round++)
        {
            sim.Press("Three Button");
            Run(sim);
            Assert.Equal(3f, scenario.Piston.Position, 3);
            Assert.True(scenario.Arrived.Enabled);
        }

        Assert.Equal(2, sim.Machine.Log.Count(line => line == "At Three:on"));
    }

    /// <summary>Cycle Button closes Old Door, moves Old Piston to 2 m and opens the door, and Opened comes on as it finishes opening, every time it is pressed.</summary>
    [Fact]
    public void TheOldAirlockCyclesEachPress()
    {
        var scenario = new OldAirlock();
        var sim = scenario.Build(BlockDefinitions.FromGame()).Sim;

        for (var round = 0; round < 2; round++)
        {
            sim.Press("Cycle Button");
            Run(sim);
            Assert.Equal(Sandbox.ModAPI.Ingame.DoorStatus.Open, scenario.Door.Status);
            Assert.Equal(2f, scenario.Piston.Position, 3);
        }

        Assert.Equal(2, sim.Machine.Log.Count(line => line == "Cycled:on"));
        Assert.Equal(2, sim.Machine.Log.Count(line => line == "Opened:on"));
    }

    /// <summary>The kept count's first logic system is kept to its programmable block, and its second is listed everywhere.</summary>
    [Fact]
    public void TheKeptCountKeepsOneSystemToItsBlock()
    {
        var scenario = new KeptCount();
        var built = scenario.Build(BlockDefinitions.FromGame());
        var systems = built.Sim.Editor.Program.Systems;

        Assert.Equal(2, systems.Count);
        Assert.Equal(built.Sim.Machine.Sprocket.Id, SystemSummary.KeptTo(built.Sim.Editor.Runner, systems[0]));
        Assert.Equal("", systems[1].ListedOn);
    }

    /// <summary>The status pages' switched-off logic systems are more than one text panel lists at once, so its automation panel turns pages.</summary>
    [Fact]
    public void TheStatusPagesFillMoreThanOnePage()
    {
        var built = new StatusPages().Build(BlockDefinitions.FromGame());
        var sim = built.Sim;
        Assert.Equal(StatusPages.Systems.Count, sim.Editor.Program.Systems.Count);
        Assert.All(sim.Editor.Program.Systems, system => Assert.False(system.Enabled));

        var status = new Features.StatusPanel.ConstructStatus();
        status.Begin(sim.Now);
        status.Include(sim.Unit.Program, sim.Runner);
        status.End("Pages");

        var face = StatusPictureTests.Face(Sim.Content.ScreenShape.Reference.First(each => each.Name == "lcd-panel"));
        var picture = new Features.StatusPanel.StatusPicture();
        var panel = Sim.Simulation.StatusFace.Panel(picture, face);
        Sim.Simulation.StatusFace.Lay(picture, panel, face, status, sim.Texts, 0, 0);
        var first = picture.Pages;
        Sim.Simulation.StatusFace.Lay(picture, panel, face, status, sim.Texts, 0, 0);

        Assert.True(picture.Pages > 1, "the panel lists every logic system on one page, so it never turns");
        Assert.Equal(picture.Pages, first);
    }

    /// <summary>Each of the script limit's buttons runs its own script and switches its light on, every time it is pressed.</summary>
    [Fact]
    public void TheScriptLimitRunsEachScriptEachPress()
    {
        var sim = new ScriptLimit().Build(BlockDefinitions.FromGame()).Sim;

        for (var round = 0; round < 2; round++)
        {
            sim.Press("Looping Button");
            Run(sim);
            sim.Press("Plain Button");
            Run(sim);
        }

        foreach (var script in new[] { "Looping", "Plain" })
        {
            Assert.Equal(2, sim.Machine.Log.Count(line => line == script + " Script:run go"));
            Assert.Equal(2, sim.Machine.Log.Count(line => line == script + " Ran:on"));
        }
    }

    /// <summary>Force Button opens the forced door every time it is pressed, from shut.</summary>
    [Fact]
    public void TheForcedDoorOpensEachPress()
    {
        var scenario = new ForcedDoor();
        var sim = scenario.Build(BlockDefinitions.FromGame()).Sim;

        for (var round = 0; round < 2; round++)
        {
            sim.Press("Force Button");
            Run(sim);
            Assert.Equal(Sandbox.ModAPI.Ingame.DoorStatus.Open, scenario.Door.Status);
            scenario.Door.Perform("Open_Off", new List<Literal>());
            Run(sim);
            Assert.Equal(Sandbox.ModAPI.Ingame.DoorStatus.Closed, scenario.Door.Status);
        }
    }

    /// <summary>Locking the dock's connector and plate leaves both lights on, and unlocking each switches its own light off.</summary>
    [Fact]
    public void TheDockUnlocksSwitchEachLightOffAsItsBlockUnlocks()
    {
        var scenario = new DockUnlocks();
        var sim = scenario.Build(BlockDefinitions.FromGame()).Sim;
        scenario.Connector.Lock(MyShipConnectorStatus.Connectable);
        scenario.Plate.Lock(LandingGearMode.ReadyToLock);
        Run(sim);

        scenario.Connector.Lock(MyShipConnectorStatus.Connected);
        scenario.Plate.Lock(LandingGearMode.Locked);
        Run(sim);
        Assert.True(scenario.ConnectorLight.Enabled && scenario.PlateLight.Enabled, "a light went off as its block locked");

        scenario.Connector.Lock(MyShipConnectorStatus.Connectable);
        Run(sim);
        Assert.False(scenario.ConnectorLight.Enabled, "the connector unlocked and its light stayed on");
        Assert.True(scenario.PlateLight.Enabled, "the plate's light went off as the connector unlocked");

        scenario.Plate.Lock(LandingGearMode.Unlocked);
        Run(sim);
        Assert.False(scenario.PlateLight.Enabled, "the plate unlocked and its light stayed on");
    }

    /// <summary>The cockpit's first toolbar slot, pressed as a seated player presses it, switches the light off.</summary>
    [Fact]
    public void TheCockpitPressSwitchesTheLightOff()
    {
        var scenario = new CockpitPress();
        var sim = scenario.Build(BlockDefinitions.FromGame()).Sim;
        Assert.True(scenario.Light.Enabled, "the light is off before the press");

        scenario.Cockpit.Press(0);
        Run(sim);

        Assert.False(scenario.Light.Enabled, "pressing the cockpit's first slot left the light on");
    }

    /// <summary>Store Button moves the ingots out of the refinery's output, its second inventory, into the cargo container.</summary>
    [Fact]
    public void TheRefineryMovesStoreTheOutput()
    {
        VRage.ObjectBuilders.MyObjectBuilderType.RegisterFromAssembly(typeof(VRage.Game.MyObjectBuilder_Ingot).Assembly);
        var scenario = new RefineryMoves();
        var sim = scenario.Build(BlockDefinitions.FromGame()).Sim;

        scenario.Refinery.Holds("Iron", 500, 1);
        Run(sim);
        sim.Press("Store Button");
        Run(sim);

        Assert.Equal(0, scenario.Refinery.Holding("Iron", 1));
        Assert.Equal(500, scenario.Cargo.Holding("Iron"));
    }

    /// <summary>A refinery switched off still has its output moved: the conveyors carry items through a block switched off.</summary>
    [Fact]
    public void TheRefineryMovesStoreTheOutputOfASwitchedOffRefinery()
    {
        VRage.ObjectBuilders.MyObjectBuilderType.RegisterFromAssembly(typeof(VRage.Game.MyObjectBuilder_Ingot).Assembly);
        var scenario = new RefineryMoves();
        var sim = scenario.Build(BlockDefinitions.FromGame()).Sim;

        scenario.Refinery.Enabled = false;
        scenario.Refinery.Holds("Iron", 500, 1);
        Run(sim);
        sim.Press("Store Button");
        Run(sim);

        Assert.Equal(0, scenario.Refinery.Holding("Iron", 1));
        Assert.Equal(500, scenario.Cargo.Holding("Iron"));
    }

    /// <summary>Ingots Button moves 100 kg of iron ingots on each press, All Button the rest, and Empty Button moves on with nothing left to move.</summary>
    [Fact]
    public void TheMoveAmountsMoveWhatEachButtonSays()
    {
        VRage.ObjectBuilders.MyObjectBuilderType.RegisterFromAssembly(typeof(VRage.Game.MyObjectBuilder_Ingot).Assembly);
        var scenario = new MoveAmounts();
        var sim = scenario.Build(BlockDefinitions.FromGame()).Sim;
        scenario.Source.Holds("Iron", 500);
        Run(sim);

        sim.Press("Ingots Button");
        Run(sim);
        Assert.Equal(100, scenario.Target.Holding("Iron"));
        sim.Press("Ingots Button");
        Run(sim);
        Assert.Equal(200, scenario.Target.Holding("Iron"));
        Assert.Equal(300, scenario.Source.Holding("Iron"));

        sim.Press("All Button");
        Run(sim);
        Assert.Equal(0, scenario.Source.Holding("Iron"));
        Assert.Equal(500, scenario.Target.Holding("Iron"));

        for (var press = 0; press < 2; press++)
        {
            scenario.MovedOn.Enabled = false;
            sim.Press("Empty Button");
            Run(sim);
            Assert.True(scenario.MovedOn.Enabled, "the move with nothing to move held");
        }
    }

    static void Run(Simulation sim)
    {
        for (var i = 0; i < 400; i++)
            sim.Tick();
    }

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

    /// <summary>A construct parting closes a screen open on the automation it kept, which opens again with nothing to take back.</summary>
    [Fact]
    public void APartingClosesTheScreenOnWhatStays()
    {
        var scenario = new SplitTwin();
        var built = scenario.Build(BlockDefinitions.FromGame());
        var sim = built.Sim;
        var aftHero = built.Deck.Blocks.Single(fitting => fitting.Name == "Aft Automation");
        var fore = sim.Roster.Kept(sim.Machine.Sprocket.Block)!;

        var screen = fore.Editor;
        screen.NewSystem("Lights");
        Assert.True(screen.CanUndoConstruct());

        var closed = new List<SprocketUnit>();
        sim.Roster.Dropped = unit => closed.Add(unit);
        sim.Machine.Split(built.Deck.Parts(scenario.Bridge).Single(part => part.Contains(aftHero)));
        Settle(sim);

        Assert.Contains(fore, closed);
        Assert.Same(fore, sim.Roster.Kept(sim.Machine.Sprocket.Block));
        Assert.NotSame(screen, fore.Editor);
        Assert.False(fore.Editor.CanUndoConstruct());
    }

    /// <summary>
    /// A construct joining another closes a screen open on the automation that takes the other in, which opens again with
    /// nothing to take back, and its block holds both.
    /// </summary>
    [Fact]
    public void AJoiningClosesTheScreenOnWhatTakesItIn()
    {
        var scenario = new SplitTwin();
        var built = scenario.Build(BlockDefinitions.FromGame());
        var sim = built.Sim;
        var aftHero = built.Deck.Blocks.Single(fitting => fitting.Name == "Aft Automation");
        var parted = sim.Machine.Split(built.Deck.Parts(scenario.Bridge).Single(part => part.Contains(aftHero)));
        Settle(sim);
        var fore = sim.Roster.Kept(sim.Machine.Sprocket.Block)!;

        var screen = fore.Editor;
        screen.NewSystem("Lights");
        Assert.True(screen.CanUndoConstruct());

        var closed = new List<SprocketUnit>();
        sim.Roster.Dropped = unit => closed.Add(unit);
        sim.Machine.Merge(parted);
        Settle(sim);

        Assert.Contains(fore, closed);
        Assert.Same(fore, Assert.Single(sim.Roster.All));
        Assert.NotSame(screen, fore.Editor);
        Assert.False(fore.Editor.CanUndoConstruct());

        var stored = new Automation();
        Assert.Equal("", ProgramReader.Load(BlockStorage.Stored(fore.Block, BlockStorage.ProgramKey), stored, sim.World.Values));
        var airlocks = stored.Systems.Single(system => system.Name != "Lights");
        Assert.Equal(new[] { "Fore", "Aft" }, airlocks.Instances.Select(instance => instance.Name));
    }

    /// <summary>A cycle running as its construct joins another carries on to its end, on the side that takes the other in and on the side taken in.</summary>
    [Theory]
    [InlineData("Fore")]
    [InlineData("Aft")]
    public void ACycleRunningThroughAJoinFinishes(string side)
    {
        var scenario = new SplitTwin();
        var built = scenario.Build(BlockDefinitions.FromGame());
        var sim = built.Sim;
        var aftHero = built.Deck.Blocks.Single(fitting => fitting.Name == "Aft Automation");
        var parted = sim.Machine.Split(built.Deck.Parts(scenario.Bridge).Single(part => part.Contains(aftHero)));
        Settle(sim);

        var log = sim.Machine.Log;
        var pressed = log.Count;
        sim.Press(side + " Button");
        for (var i = 0; i < 600 && !log.Skip(pressed).Contains(side + " Outer Door:open"); i++)
            sim.Tick();
        Assert.DoesNotContain(side + " Inner Door:open", log.Skip(pressed));

        sim.Machine.Merge(parted);
        for (var i = 0; i < 600; i++)
            sim.Tick();

        Assert.Contains(side + " Inner Door:open", log.Skip(pressed));
    }

    /// <summary>
    /// A construct joining another runs the joined program's own sequences, on every installation, once it has ticked, and
    /// what each run is doing stays in the construct's memory, which a save writes.
    /// </summary>
    [Fact]
    public void AJoinedAutomationRunsItsOwnSequences()
    {
        var scenario = new SplitTwin();
        var built = scenario.Build(BlockDefinitions.FromGame());
        var sim = built.Sim;
        var aftHero = built.Deck.Blocks.Single(fitting => fitting.Name == "Aft Automation");
        var parted = sim.Machine.Split(built.Deck.Parts(scenario.Bridge).Single(part => part.Contains(aftHero)));
        Settle(sim);

        sim.Machine.Merge(parted);
        Settle(sim);

        var joined = Assert.Single(sim.Roster.All);
        var system = joined.Program.Systems.Single();
        foreach (var instance in system.Instances)
        {
            foreach (var sequence in system.Sequences)
            {
                var run = joined.Runner.Get(sequence, instance);
                Assert.Same(sequence, run.Sequence);
                Assert.Contains(run.Memory, joined.Memory.Runs);
            }
        }
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

        var program = blueprint.Descendants("item").Single(item => (string)item.Element("Key") == BlockStorage.ProgramKey.ToString("D"));
        var read = new Automation();
        Assert.Equal("", ProgramReader.Load((string)program.Element("Value")!, read, built.Sim.World.Values));

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
            Assert.Equal((long)panel.Element("EntityId")!, (long)data.Element("BlockEntityId")!);
        }
    }
}
