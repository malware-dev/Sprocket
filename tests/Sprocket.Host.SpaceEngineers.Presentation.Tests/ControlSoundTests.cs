using System.IO;
using System.Xml.Linq;
using Lumel;
using Lumel.Input;
using Lumel.Sim;
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

    /// <summary>
    /// The hover and click cues are the game's own HUD sounds, the ones its menus play for the pointer coming onto a control and
    /// pressing it, so the editor sounds as loud as the menus where the game's content is here.
    /// </summary>
    [Fact]
    public void TheCuesAreTheGamesOwnHudSounds()
    {
        if (GameContent.ContentFolder == null)
            return;

        var audio = XDocument.Load(Path.Combine(GameContent.ContentFolder, "Data", "Audio_gui.sbc"));
        foreach (var cue in new[] { ControlCues.Hover, ControlCues.Click })
        {
            var sound = audio.Descendants("Sound").SingleOrDefault(s => (string?)s.Element("Id")?.Element("SubtypeId") == cue.Name);
            Assert.True(sound != null, $"the game defines no sound {cue.Name}");
            Assert.Equal("HUD", (string?)sound!.Element("Category"));
        }
    }

    /// <summary>Everything shown that a press would run sounds as the pointer comes onto it and as it is pressed.</summary>
    [Theory]
    [MemberData(nameof(Screens))]
    public void EverythingPressableSounds(string script)
    {
        var screen = Screen.Fresh(Script(script));

        var silent = screen.Scene.View.Within()
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
