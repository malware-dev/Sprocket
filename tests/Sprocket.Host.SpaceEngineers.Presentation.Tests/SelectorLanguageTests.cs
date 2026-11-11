using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A selector open while the game's language changes says everything in the new language.</summary>
public class SelectorLanguageTests
{
    const string Door = "Airlock Inner Door";

    /// <summary>A selector on a door's open, switched to Norwegian, draws what one opened in Norwegian draws, its option tips included.</summary>
    [Fact]
    public void ASelectorSwitchedToNorwegianSaysWhatAFreshOneSays()
    {
        var switched = Stepping();
        switched.Editor.PressAdd("task");
        switched.Editor.PressBlock(Door);
        switched.Run(2);
        var at = switched.Editor.Members().IndexOf("open");
        switched.Editor.PressChoice("open");
        switched.Run(2);
        switched.Scene.Speak("no");
        switched.Run(2);

        var fresh = Stepping();
        fresh.Scene.Speak("no");
        fresh.Run(2);
        fresh.Editor.PressAdd(fresh.Editor.Texts.SequenceDoSomething);
        fresh.Editor.PressBlock(Door);
        fresh.Run(2);
        fresh.Editor.PressChoice(fresh.Editor.Members()[at]);
        fresh.Run(2);

        // The slot the step makes keeps the name it was given when the selector opened, as every name given does.
        var expected = fresh.Words.Select(word => word == "blokkplass 1" ? "block slot 1" : word).ToList();
        Assert.Equal(expected, switched.Words);
        Assert.Equal(Tips(fresh.Editor), Tips(switched.Editor));
    }

    /// <summary>The editor on a fresh construct, in a new button system's sequence.</summary>
    static Screen Stepping()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Words");
        screen.Run(2);
        return screen;
    }

    /// <summary>The tips on the names of the options of the action chosen.</summary>
    static List<string> Tips(SprocketEditor editor) =>
        editor.Pick().Arguments!.Form.Within().OfType<Label>().Select(label => label.Tip?.Text ?? "").ToList();
}
