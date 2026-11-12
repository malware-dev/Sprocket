using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A switched-off logic system runs nothing, so a sequence's play button is held in its place and says which switch to turn on.</summary>
public class PlayButtonTests
{
    /// <summary>A sequence's row on its switched-off logic system holds its play button, which runs the sequence once the system is switched on.</summary>
    [Fact]
    public void ARowOfASwitchedOffSystemHoldsItsPlayButton()
    {
        var screen = Screen.On(Rig.Demo());
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        var system = editor.Program.Systems[0];
        editor.GoToRoot();
        screen.Run(2);
        system.Enabled = false;
        editor.Program.Edited();
        editor.OpenBlueprintSystem(0);
        screen.Run(2);

        var row = editor.Rows().First(each => each.Runs != null);
        Assert.Equal(editor.Texts.Codex.SequenceRunSystemOff, row.RunLocked);

        editor.SwitchSystem(system, true);
        screen.Run(2);

        row = editor.Rows().First(each => each.Runs != null);
        Assert.Null(row.RunLocked);
    }
}
