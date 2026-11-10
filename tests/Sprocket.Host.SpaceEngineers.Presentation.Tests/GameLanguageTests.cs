using Sprocket.Sim.Content;
using Sprocket.Sim.Machine;
using Sprocket.Host.SpaceEngineers.World;
using VRage.ObjectBuilders;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Blocks whose terminal speaks the game's own texts in another language, read from the game's content.</summary>
/// <remarks>Each needs the game installed, or the harness's cache of its content, and passes without looking where neither is here.</remarks>
public class GameLanguageTests
{
    /// <summary>A flight setting of a remote control, by its terminal id, beside the key of the game's text that titles it, and whether the game gives it actions that switch it on and off.</summary>
    /// <remarks>From the game's ship controller terminal controls.</remarks>
    static readonly (string Id, string Title, bool Switched)[] Flight =
    {
        ("HandBrake", "TerminalControlPanel_Cockpit_Handbrake", true),
        ("DampenersOverride", "ControlName_InertialDampeners", true),
        ("ControlThrusters", "TerminalControlPanel_Cockpit_ControlThrusters", false)
    };

    /// <summary>
    /// A remote control titling its handbrake, dampeners and thrusters in the game's Norwegian shows each by Sprocket's own name
    /// and describes it in Sprocket's Norwegian, in a game spoken in Norwegian.
    /// </summary>
    [Theory]
    [InlineData("handbrake")]
    [InlineData("dampeners")]
    [InlineData("control thrusters")]
    public void ARemoteControlInNorwegianNamesAndDescribesItsFlightSettings(string member)
    {
        var game = GameLanguage.In("no");
        if (game == null)
            return;

        var screen = Remote(game);
        var rows = screen.Editor.Pick().Stages[1].Entries().Select(entry => entry.Text).ToList();
        Assert.Contains(member, rows);
        Assert.DoesNotContain(rows, string.IsNullOrWhiteSpace);
        Assert.DoesNotContain(rows, row => Flight.Any(setting => row == game.Find(setting.Title)));

        screen.Editor.PressChoice(member);
        screen.Run(2);

        var about = screen.Editor.Pick().Arguments!.About;
        Assert.True(about.IsVisible, member + " is not described");
        Assert.False(string.IsNullOrWhiteSpace(about.Markdown), member + " is described with nothing");
        Assert.Contains(about.Markdown, Norwegian());
    }

    /// <summary>The game's Norwegian titles the remote control's flight settings, and none of them falls back to its key.</summary>
    [Fact]
    public void TheGameTitlesTheFlightSettingsInNorwegian()
    {
        var game = GameLanguage.In("no");
        var english = GameLanguage.In("");
        if (game == null || english == null)
            return;

        foreach (var setting in Flight)
        {
            var title = game.Find(setting.Title);
            Assert.False(string.IsNullOrEmpty(title), setting.Title);
            Assert.NotEqual(english.Find(setting.Title), title);
        }
    }

    /// <summary>A task on a remote control whose flight settings the game titles in <paramref name="game" />'s words, its block chosen, the game then spoken in Norwegian.</summary>
    static Screen Remote(ResxTexts game)
    {
        var rig = new Rig();
        rig.Panel(41, "Airlock Button");
        var remote = rig.Weld(new Remote(40, "Remote 1", new MyObjectBuilderType(typeof(Sandbox.Common.ObjectBuilders.MyObjectBuilder_RemoteControl)),
            "LargeBlockRemoteControl", "Fjernkontroll"));
        var on = game.Find("SwitchText_On");
        var off = game.Find("SwitchText_Off");
        foreach (var setting in Flight)
        {
            // The game titles a switch's toggle with its title and both its words, and each of the pair with one of them.
            var title = game.Find(setting.Title);
            remote.Offering(setting.Id, $"{title} {on}/{off}");
            if (setting.Switched)
                remote.Offering(setting.Id + "_On", $"{title} {on}").Offering(setting.Id + "_Off", $"{title} {off}");

            remote.Carrying(setting.Id, title!, ValueKind.Flag);
        }

        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Fly");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Remote 1");
        screen.Run(2);
        screen.Scene.Speak("no");
        screen.Run(2);
        return screen;
    }

    /// <summary>Every member description Sprocket says in Norwegian.</summary>
    static List<string> Norwegian()
    {
        var texts = new ResxTexts(ResxTexts.Beside, "no");
        var english = new ResxTexts(ResxTexts.Beside, "");
        var said = new List<string>();
        foreach (var key in Keys())
        {
            var text = texts.Find(key);
            if (text != null && text != english.Find(key))
                said.Add(text);
        }

        return said;
    }

    /// <summary>The keys of Sprocket's member descriptions.</summary>
    static IEnumerable<string> Keys()
    {
        var codex = System.Xml.Linq.XDocument.Load(System.IO.Path.Combine(ResxTexts.Beside, "Codex.resx"));
        return codex.Root!.Elements("data").Select(data => (string)data.Attribute("name")!).Where(key => key.StartsWith("Sprocket.Member.", StringComparison.Ordinal));
    }
}
