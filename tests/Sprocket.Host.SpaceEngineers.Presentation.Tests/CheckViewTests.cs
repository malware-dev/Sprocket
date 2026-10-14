using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>What a check can read is offered in both views, as what a step can do is; what starts a trigger has the one list.</summary>
public class CheckViewTests
{
    /// <summary>
    /// A check's organized view lists the block's states under headings, and its terminal view the values the block declares,
    /// under the terminal's own names; the organized view comes back as it was.
    /// </summary>
    [Fact]
    public void ACheckHasAnOrganizedAndATerminalView()
    {
        var rig = Airlock();
        rig.Editor.PressAdd(rig.Texts.SequenceIf);
        rig.Tick();
        rig.Editor.PressBlock("Airlock Outer Door");
        rig.Tick();

        var what = What(rig);
        Assert.Contains(what.Entries(), entry => entry.Text == "is closed");
        Assert.Contains(what.Entries(), entry => entry.IsHeading);

        what.PressView("Terminal");
        rig.Tick();

        what = What(rig);
        Assert.DoesNotContain(what.Entries(), entry => entry.Text == "is closed");
        Assert.Contains(what.Entries(), entry => !entry.IsHeading);

        what.PressView("Organized");
        rig.Tick();

        Assert.Contains(What(rig).Entries(), entry => entry.Text == "is closed");
    }

    /// <summary>The signals that start a trigger are one list, with no link to another view.</summary>
    [Fact]
    public void ASignalHasOneList()
    {
        var rig = Airlock();
        rig.Editor.Push(new LogicSystemPage(rig.Editor, rig.Editor.Program.Systems[0]));
        rig.Tick();
        rig.Editor.PressStart(rig.Editor.Texts.SystemWhen);
        rig.Tick();
        rig.Editor.PressBlock("Airlock Outer Door");
        rig.Tick();

        Assert.All(rig.Editor.Pick().Stages, stage => Assert.Empty(stage.Views));
    }

    /// <summary>The column of the selector that offers what is read or done, the one with a link to another view.</summary>
    static StageColumn What(Simulation rig)
    {
        return rig.Editor.Pick().Stages.Single(stage => stage.Views.Count > 0);
    }

    /// <summary>An airlock logic system, switched off and open on its first sequence.</summary>
    static Simulation Airlock()
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Tick();

        var system = rig.Editor.Program.Systems[0];
        system.Enabled = false;
        rig.Editor.Program.Edited();
        rig.Editor.Push(new SequencePage(rig.Editor, system.Sequences[0]));
        rig.Tick();
        return rig;
    }
}
