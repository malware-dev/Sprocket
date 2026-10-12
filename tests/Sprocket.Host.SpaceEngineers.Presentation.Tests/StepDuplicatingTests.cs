using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A step is copied from its row on the sequence's screen, and the copy lands right after it.</summary>
/// <remarks>A copy holds what a drag would carry: an if with every step in it, a branch with its own, a step with the wait after it.</remarks>
public class StepDuplicatingTests
{
    /// <summary>A step is copied right after itself, as one edit undo takes back.</summary>
    [Fact]
    public void AStepIsCopiedRightAfterItself()
    {
        var rig = Airlock(out var sequence);
        sequence.Steps.Clear();
        sequence.Steps.Add(new Wait(60));
        sequence.Steps.Add(new Wait(120));
        rig.Editor.Program.Edited();
        rig.Tick();

        rig.Editor.StepRows()[0].Copies!.Execute();
        rig.Tick();

        Assert.Equal(new[] { 60, 60, 120 }, Ticks(sequence));
        Assert.NotSame(sequence.Steps[0], sequence.Steps[1]);

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Equal(new[] { 60, 120 }, Ticks(sequence));
    }

    /// <summary>A step and the wait for it to finish are one row, and are copied together.</summary>
    [Fact]
    public void AStepIsCopiedWithTheWaitForIt()
    {
        var rig = Airlock(out var sequence);
        sequence.Steps.Clear();
        sequence.Steps.Add(new Run(2, false));
        sequence.Steps.Add(new Await());
        sequence.Steps.Add(new Wait(30));
        rig.Editor.Program.Edited();
        rig.Tick();

        rig.Editor.StepRows()[0].Copies!.Execute();
        rig.Tick();

        Assert.IsType<Run>(sequence.Steps[0]);
        Assert.IsType<Await>(sequence.Steps[1]);
        Assert.IsType<Run>(sequence.Steps[2]);
        Assert.IsType<Await>(sequence.Steps[3]);
        Assert.IsType<Wait>(sequence.Steps[4]);
    }

    /// <summary>An if is copied with every branch and step in it, after its end.</summary>
    [Fact]
    public void AnIfIsCopiedWithItsSteps()
    {
        var rig = Airlock(out var sequence);
        sequence.Steps.Clear();
        sequence.Steps.Add(new If(Closed(rig)));
        sequence.Steps.Add(new Wait(60));
        sequence.Steps.Add(new Else(null!));
        sequence.Steps.Add(new Wait(120));
        sequence.Steps.Add(new EndIf());
        sequence.Steps.Add(new Wait(30));
        rig.Editor.Program.Edited();
        rig.Tick();

        rig.Editor.StepRows()[0].Copies!.Execute();
        rig.Tick();

        Assert.Equal(11, sequence.Steps.Count);
        Assert.IsType<If>(sequence.Steps[5]);
        Assert.NotSame(((If)sequence.Steps[0]).Test, ((If)sequence.Steps[5]).Test);
        Assert.IsType<Else>(sequence.Steps[7]);
        Assert.IsType<EndIf>(sequence.Steps[9]);
        Assert.Equal(30, ((Wait)sequence.Steps[10]).Ticks);
    }

    /// <summary>An otherwise if is copied with its steps, as another branch right after it; a plain otherwise and an if's end have no copy.</summary>
    [Fact]
    public void ABranchIsCopiedAsAnotherBranch()
    {
        var rig = Airlock(out var sequence);
        sequence.Steps.Clear();
        sequence.Steps.Add(new If(Closed(rig)));
        sequence.Steps.Add(new Wait(60));
        sequence.Steps.Add(new Else(Closed(rig)));
        sequence.Steps.Add(new Wait(90));
        sequence.Steps.Add(new Else(null!));
        sequence.Steps.Add(new Wait(120));
        sequence.Steps.Add(new EndIf());
        rig.Editor.Program.Edited();
        rig.Tick();

        var rows = rig.Editor.StepRows();
        Assert.Null(rows.First(row => row.Kind == StepKind.Otherwise).Copies);
        Assert.Null(rows.First(row => row.Kind == StepKind.End).Copies);

        rows.First(row => row.Kind == StepKind.OtherwiseIf).Copies!.Execute();
        rig.Tick();

        Assert.Equal(9, sequence.Steps.Count);
        Assert.NotNull(((Else)sequence.Steps[4]).Test);
        Assert.Equal(90, ((Wait)sequence.Steps[5]).Ticks);
        Assert.Null(((Else)sequence.Steps[6]).Test);
    }

    /// <summary>Copying is an edit, so a step of a logic system switched on is not copied.</summary>
    [Fact]
    public void AStepOfARunningSystemIsNotCopied()
    {
        var rig = Airlock(out var sequence);
        rig.Editor.SwitchSystem(sequence.System, true);
        rig.Tick();

        Assert.All(rig.Editor.StepRows(), row => Assert.Null(row.Copies));
    }

    static int[] Ticks(Sequence sequence) =>
        sequence.Steps.Select(step => ((Wait)step).Ticks).ToArray();

    static Simulation Airlock(out Sequence sequence)
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Tick();

        var system = rig.Editor.Program.Systems[0];
        sequence = system.Sequences[0];
        system.Enabled = false;
        rig.Editor.Program.Edited();

        rig.Editor.GoToRoot();
        rig.Tick();
        rig.Editor.Push(new SequencePage(rig.Editor, sequence));
        rig.Tick();
        return rig;
    }

    static Comparison Closed(Simulation rig)
    {
        var door = rig.World.Entities.First(entity => entity.Name == "Airlock Outer Door");
        var test = new Comparison { MemberId = "status", Provider = "ext", Value = Literal.Name("closed") };
        test.Targets.Add(rig.Editor.Program.Declare(rig.World, door).Name);
        return test;
    }
}
