using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>An action's options say what they do in a tip on their names, with their choices in code style, in both languages.</summary>
public class OptionTipTests
{
    /// <summary>A door's open says what force does, and a vent's pressurize what when stuck does, each tip opening whole on its name.</summary>
    [Theory]
    [InlineData("", "Airlock Inner Door", "open", "`normal` stops")]
    [InlineData("no", "Airlock Inner Door", "open", "`normal` stopper")]
    [InlineData("", "Airlock Vent", "pressurize", "`hold` waits")]
    [InlineData("no", "Airlock Vent", "pressurize", "`hold` venter")]
    public void AnOptionsTipSaysItsChoices(string language, string block, string action, string opens)
    {
        var screen = Screen.WithGameFonts(Rig.Demo());
        Assert.True(screen.HasGameFonts, "the game's font was not found, so nothing was measured as the game measures it");
        var editor = screen.Editor;
        editor.NewButtonSystem("Tips");
        screen.Run(2);

        // The action is found by its place in the block's list, which is the same in every language.
        editor.PressAdd("task");
        editor.PressBlock(block);
        screen.Run(2);
        var at = editor.Members().IndexOf(action);
        editor.Back();
        screen.Run(2);
        if (language.Length > 0)
        {
            screen.Scene.Speak(language);
            screen.Run(2);
        }

        editor.PressAdd(editor.Texts.SequenceDoSomething);
        editor.PressBlock(block);
        screen.Run(2);
        editor.PressChoice(editor.Members()[at]);
        screen.Run(2);

        var name = editor.Pick().Arguments!.Form.Within().OfType<Label>().SingleOrDefault(label => label.Tip?.Text.StartsWith(opens) == true);
        Assert.True(name != null, $"no option of {action} has a tip opening with {opens}");

        var middle = name.ToScreen(name.Bounds.Center);
        screen.Do("move", ((int)middle.X).ToString(), ((int)middle.Y).ToString(), "tick", (Styles.TipDelay * 2).ToString());
        Assert.Same(name.Tip, screen.Scene.ShownTip);
        Assert.Empty(screen.Cut);
        Assert.Empty(screen.Overlapping);
    }
}
