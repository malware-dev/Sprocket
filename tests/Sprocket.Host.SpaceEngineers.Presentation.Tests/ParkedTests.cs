using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRageMath;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A logic system Sprocket switched off says why, on its row and on its own screen, with a ground.
/// </summary>
/// <remarks>
/// Once what broke it is mended, it is the likeliest reason a button does nothing, and everything
/// else about it reads as working: the trigger is right and the blocks are there.
/// </remarks>
public class ParkedTests
{
    const string Why = "Stopped at step 1 of Sequence 1: Airlock Inner Door was lost but is now restored.";

    static Screen WithOneStopped()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("close");
        screen.Editor.PressPrimary();
        screen.Editor.GoToRoot();

        // Sprocket stops an installation, not the logic system over every installation of it.
        var system = screen.Editor.Program.Systems[0];
        var installation = system.First!;
        system.Enabled = true;
        installation.Enabled = false;
        installation.Halt = new Halt(SequenceProblem.MissingTarget, system.Sequences[0].Number, 0, BlockTrouble.None, "Airlock Inner Door");
        return screen.Run(120);
    }

    static bool Washed(Screen screen, Color ground) =>
        screen.Drawings.Any(d => !d.IsText && d.Ink == ground);

    /// <summary>Its row says why, in the line it already has for what it is doing, on the parked ground.</summary>
    [Fact]
    public void ItsRowSaysWhy()
    {
        var screen = WithOneStopped();
        var row = screen.Editor.RootRows()[0];

        Assert.Equal(Why, row.Problem);
        Assert.True(row.Parked);
        Assert.True(screen.Shows(Why), "the row does not say why it was switched off");
        Assert.True(Washed(screen, Palette.NoticeWash), "a system Sprocket switched off has no ground");
    }

    /// <summary>What broke it is mended, so it is not counted as broken or washed as broken.</summary>
    [Fact]
    public void BeingStoppedIsNotAFault()
    {
        var screen = WithOneStopped();

        Assert.Equal(0, screen.Editor.RootRows()[0].Problems);
        Assert.False(Washed(screen, Palette.ProblemWash), "a mended system's row reads as broken");
    }

    /// <summary>Its own screen says why as well, above everything it is about.</summary>
    [Fact]
    public void ItsScreenSaysWhy()
    {
        var screen = WithOneStopped();
        screen.Editor.PressRow(0);
        screen.Run(120);

        Assert.True(screen.Shows(Why), "the logic system screen does not say why it was switched off");
    }

    /// <summary>Switching it on takes the ground and the words away.</summary>
    [Fact]
    public void SwitchingItOnClearsIt()
    {
        var screen = WithOneStopped();

        screen.Editor.RootRows()[0].Flips!.Execute();
        screen.Run(120);

        Assert.Null(screen.Editor.Program.Systems[0].First!.Halt);
        Assert.False(screen.Editor.RootRows()[0].Parked);
        Assert.Equal("", screen.Editor.RootRows()[0].Problem);
        Assert.False(screen.Shows(Why), "the words stayed after it was switched on");
    }

    /// <summary>A system Sprocket did not switch off says nothing about it: its switch already does.</summary>
    [Fact]
    public void ASystemSwitchedOffByHandIsNotRemarkedOn()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Editor.GoToRoot();
        screen.Run(120);

        Assert.False(screen.Editor.Program.Systems[0].Enabled);
        Assert.False(screen.Editor.RootRows()[0].Parked);
        Assert.Equal("", screen.Editor.RootRows()[0].Problem);
    }
}
