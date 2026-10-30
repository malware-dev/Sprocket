using Lumel;
using Lumel.Input;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>What a pressable thing sounds like.</summary>
public class ControlSoundTests
{
    /// <summary>Each screen of the reference suite, by the script that reaches it.</summary>
    public static TheoryData<string> Screens => new()
    {
        "",
        "press \"+ logic system\" type Airlock enter",
        "press \"+ logic system\" type Airlock enter press Sequences press \"+ sequence\" press \"+ step\" press task press \"Airlock Inner Door\" press open press Done",
        "press \"+ logic system\" type Airlock enter press Sequences press \"+ sequence\" press \"+ step\" press task type Standby press \"Standby Battery\" press \"Terminal ›\" press \"Charge Mode\"",
        "press Library",
        "press \"+ logic system\" type Airlock enter press \"Installation panel\" press Markup"
    };

    /// <summary>Everything shown that a press would run sounds as the pointer comes onto it and as it is pressed.</summary>
    [Theory]
    [MemberData(nameof(Screens))]
    public void EverythingPressableSounds(string script)
    {
        var screen = Screen.Fresh(Script(script));

        var silent = screen.Scene.View.Root.Within()
            .Where(e => e.Command != null && Shown(e))
            .Where(e => e.SoundFor(ControlSound.Hover) != ControlCues.Hover || e.SoundFor(ControlSound.Click) != ControlCues.Click)
            .Select(e => e.GetType().Name)
            .ToList();

        Assert.Empty(silent);
    }

    static bool Shown(Element element)
    {
        for (var at = element; at != null; at = at.Parent)
        {
            if (!at.IsVisible)
                return false;
        }

        return true;
    }

    /// <summary>The words of <paramref name="script" />, split at each space as the suite splits its lines.</summary>
    static string[] Script(string script) => script.Length == 0 ? [] : script.Split(' ');
}
