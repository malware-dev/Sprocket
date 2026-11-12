using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Deleting a sequence is a step its logic system's undo takes back, and redo makes again.</summary>
public class SequenceDeleteTests
{
    /// <summary>A sequence deleted among others comes back in its place, still run by its trigger, and runs; redo deletes it again.</summary>
    [Fact]
    public void ADeletedSequenceComesBackInItsPlaceAndRuns()
    {
        var screen = Screen.On(Rig.Demo());
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        var system = editor.Program.Systems[0];
        var first = system.Sequences[0];
        Off(screen, "Airlock Light");
        editor.BackKeeping();
        screen.Run(2);
        editor.NewSequence(system);
        screen.Run(2);
        Off(screen, "Cockpit Light");
        editor.GoToRoot();
        screen.Run(2);
        system.Enabled = false;
        editor.Program.Edited();
        editor.OpenBlueprintSystem(0);
        screen.Run(2);
        var second = system.Sequences[1];

        editor.Rows().First(row => row.Summary == Called(first)).Deletes!.Execute();
        screen.Run(2);
        Assert.Equal(new[] { second }, system.Sequences);
        Assert.Empty(system.Triggers);

        editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Equal(new[] { first, second }, system.Sequences);
        Assert.Equal(first.Number, Assert.Single(system.Triggers).Runs);

        var light = (Sandbox.ModAPI.Ingame.IMyFunctionalBlock)editor.World.Entities.First(e => e.Name == "Airlock Light").Block!;
        Assert.True(light.Enabled, "the light was off before the run, so this proves nothing");
        system.Enabled = true;
        screen.Run(2);
        editor.RunNow(first);
        screen.Run(5);
        Assert.False(light.Enabled, "the sequence brought back did not run");
        system.Enabled = false;
        screen.Run(2);

        editor.Screen.Redo!.Execute();
        screen.Run(2);
        Assert.Equal(new[] { second }, system.Sequences);
    }

    /// <summary>What the sequence's row calls it.</summary>
    static string Called(Sequence sequence) => sequence.HasName ? sequence.Name : "Sequence " + sequence.Number;

    /// <summary>Adds a step to the sequence open, switching <paramref name="light" /> off.</summary>
    static void Off(Screen screen, string light)
    {
        screen.Editor.PressAdd("task");
        screen.Run(2);
        screen.Editor.PressBlock(light);
        screen.Editor.PressChoice("power");
        screen.Editor.Pick().Arguments!.Form.Fields[0].Text = "off";
        screen.Editor.PressPrimary();
        screen.Run(2);
    }
}
