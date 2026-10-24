using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Library;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Two versions of a logic system that met when two constructs joined wait, switched off, for the player to keep one.</summary>
public class VersionChoiceTests
{
    /// <summary>Both headings offer the choice, and count it as a problem.</summary>
    [Fact]
    public void BothVersionsOfferTheChoice()
    {
        var sim = Waiting(out _, out _);

        var headings = sim.Editor.RootHeadings();
        Assert.Equal(2, headings.Count);
        foreach (var heading in headings)
        {
            Assert.Equal(sim.Texts.SystemsChooseVersion, heading.ButtonText);
            Assert.True(heading.Problems > 0, "waiting for a choice is not shown as a problem");
        }
    }

    /// <summary>Keeping the version that arrived puts it under the identity the buttons name, with the installations of both, in one change undo takes back.</summary>
    [Fact]
    public void KeepingOneLeavesItWithBothInstallations()
    {
        var sim = Waiting(out var here, out var waiting);
        var identity = here.Identity;

        sim.Editor.RootHeadings()[0].Offers!.Execute();
        sim.Tick();
        Assert.Contains("Which version of", sim.Editor.Question());
        sim.Editor.PressChoice(Template.Fill(sim.Texts.ConflictKeep, "date", LibraryPage.Time(waiting.Changed!.Value)));
        sim.Tick();

        var kept = Assert.Single(sim.Editor.Program.Systems);
        Assert.Equal(identity, kept.Identity);
        Assert.Equal("Cycle", kept.Sequences[0].Name);
        Assert.Equal(2, kept.Instances.Count);
        Assert.False(kept.Enabled);
        Assert.Empty(sim.Unit.Memory.Conflicts);
        Assert.NotEqual(sim.Texts.SystemsChooseVersion, sim.Editor.RootHeadings()[0].ButtonText);

        Assert.Equal(Filled.With(sim.Texts.ChangeChooseVersion, "thing", "Airlock"), sim.Editor.Screen.UndoText);
        sim.Editor.Screen.Undo!.Execute();
        sim.Tick();

        Assert.Equal(2, sim.Editor.Program.Systems.Count);
        Assert.Single(sim.Unit.Memory.Conflicts);
    }

    /// <summary>Switching one of the versions on asks which to keep, and leaves both off.</summary>
    [Fact]
    public void SwitchingOneOnAsksWhichToKeep()
    {
        var sim = Waiting(out var here, out _);

        sim.Editor.SwitchSystem(here, true);
        sim.Tick();

        Assert.Contains("Which version of", sim.Editor.Question());
        Assert.False(here.Enabled);
    }

    /// <summary>A copy lent to a client to edit knows which versions wait, so a player on a server is offered the choice too.</summary>
    [Fact]
    public void ACopyBeingEditedKnowsWhichVersionsWait()
    {
        var sim = Waiting(out var here, out _);

        string program;
        string state;
        sim.Unit.Written(out program, out state);
        var copy = SprocketUnit.Copy(sim.Unit.Block, new Settings(developerMode: false), sim.Kinds, sim.Texts, program, state);

        Assert.NotNull(copy.Editor.Versions.Rival(copy.Program.FindSystem(here.Identity)!));
    }

    /// <summary>
    /// The airlock logic system with another version of it beside, waiting: the one that arrived renames its sequence and has
    /// an installation on the hangar door.
    /// </summary>
    static Simulation Waiting(out AutomationSystem here, out AutomationSystem waiting)
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Airlock Inner Door");
        sim.Editor.PressChoice("open");
        sim.Editor.PressPrimary();
        sim.Tick();

        var program = sim.Editor.Program;
        here = program.Systems[0];
        here.Changed = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var identity = Guid.NewGuid();
        Assert.Equal("", ProgramReader.AddBlueprint(ProgramWriter.Blueprint(here, identity), program, sim.World.Values));
        waiting = program.FindSystem(identity)!;
        waiting.Sequences[0].Name = "Cycle";
        waiting.Changed = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);
        waiting.Enabled = false;

        var first = here.First!;
        var slot = first.Filled.First(filled => first.Positions(filled) == null);
        var hangar = program.Declare(sim.World, sim.World.Entities.First(entity => entity.Name == "Hangar Door"));
        waiting.AddInstance().Fill(slot, new[] { hangar.Name });

        sim.Unit.Memory.Conflicts.Add(new ConflictMemory { System = here.Identity, Waiting = identity });
        program.Edited();
        sim.Editor.GoToRoot();
        sim.Tick();
        return sim;
    }
}
