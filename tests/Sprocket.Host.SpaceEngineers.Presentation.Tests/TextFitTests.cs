using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using System.IO;
using Sprocket.Sim.Content;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Every screen of a tour through the editor, in English and in Norwegian, measured with the game's own letter widths: no
/// words run past the side of what holds them, and no two runs of words are drawn over each other.
/// </summary>
/// <remarks>Needs the game installed, for its font; without it the test says so rather than passing.</remarks>
public class TextFitTests
{
    /// <summary>The tour, each stop a name and the presses that reach it from a construct that remembers nothing.</summary>
    public static IEnumerable<object[]> Stops => new[]
    {
        new object[] { "home", new string[0] },
        new object[] { "system", new[] { "press", "+ logic system", "type", "Airlock", "enter" } },
        new object[] { "sequence", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "Sequences", "press", "+ sequence", "press", "+ step", "press", "task", "press", "Airlock Inner Door", "press", "=open", "press", "Done" } },
        new object[] { "picker", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "Sequences", "press", "+ sequence", "press", "+ step", "press", "task", "type", "Standby", "press", "Standby Battery", "press", "Terminal ›", "press", "Charge Mode" } },
        new object[] { "library", new[] { "press", "Library" } },
        new object[] { "guide", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "Installation panel", "press", "Markup" } },
        new object[] { "home with a system", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "Done" } },
        new object[] { "installation panel", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "Installation panel" } },
        new object[] { "state", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "State" } },
        new object[] { "step menu", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "Sequences", "press", "+ sequence", "press", "+ step" } },
        new object[] { "if selector", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "Sequences", "press", "+ sequence", "press", "+ step", "press", "=if" } },
        new object[] { "if on a door", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "Sequences", "press", "+ sequence", "press", "+ step", "press", "=if", "press", "Airlock Inner Door" } },
        new object[] { "delay", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "Sequences", "press", "+ sequence", "press", "+ step", "press", "delay" } },
        new object[] { "trigger menu", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "+ trigger" } },
        new object[] { "when selector", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "+ trigger", "press", "when" } },
        new object[] { "when on a door", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "+ trigger", "press", "when", "press", "Airlock Inner Door" } },
        new object[] { "every", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "+ trigger", "press", "every" } },
        new object[] { "library with a blueprint", new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "Done", "press", "Make blueprint" } }
    };

    /// <summary>Each stop of the tour fits, in both languages.</summary>
    [Theory]
    [MemberData(nameof(Stops))]
    public void EveryStopFits(string stop, string[] presses)
    {
        var found = new List<string>();
        var english = Screen.WithGameFonts(Rig.Demo(), presses.Select(Literal).ToArray());
        Assert.True(english.HasGameFonts, "the game's font was not found, so nothing was measured as the game measures it");
        Look(english, stop + ", English", found);

        // Spoken in Norwegian from the start, as a game in Norwegian loads, and pressed by the Norwegian words.
        var norwegian = Screen.WithGameFonts(Rig.Demo());
        norwegian.Scene.Speak("no");
        norwegian.Run(2);
        norwegian.Do(Norwegian(presses));
        Look(norwegian, stop + ", Norwegian", found);

        Assert.True(found.Count == 0, string.Join("\n", found));
    }

    /// <summary>The tip of every word on each stop of the tour, held over until it opens, fits, in both languages.</summary>
    [Theory]
    [MemberData(nameof(Stops))]
    public void EveryTipFits(string stop, string[] presses)
    {
        var found = new List<string>();
        var english = Screen.WithGameFonts(Rig.Demo(), presses.Select(Literal).ToArray());
        Assert.True(english.HasGameFonts, "the game's font was not found, so nothing was measured as the game measures it");
        var shown = Tips(english, stop + ", English", found);

        var norwegian = Screen.WithGameFonts(Rig.Demo());
        norwegian.Scene.Speak("no");
        norwegian.Run(2);
        norwegian.Do(Norwegian(presses));
        shown += Tips(norwegian, stop + ", Norwegian", found);

        Assert.True(shown > 0, "no word on " + stop + " opened a tip");
        Assert.True(found.Count == 0, string.Join("\n", found.Distinct()));
    }

    /// <summary>Holds the pointer over each word <paramref name="screen" /> shows until its tip opens, looks, and says how many opened one.</summary>
    static int Tips(Screen screen, string where, List<string> into)
    {
        var words = screen.Drawings.Where(drawn => drawn.IsText && !drawn.IsSymbol && drawn.Text.Trim().Length > 0 && screen.IsInsideItsClip(drawn))
            .Select(drawn => new { drawn.Text, At = Screen.Ui(drawn.At.X + 4f, screen.MiddleOf(drawn)) }).ToList();
        var opened = 0;
        foreach (var word in words)
        {
            var at = word.At;
            screen.Do("move", ((int)at.X).ToString(), ((int)at.Y).ToString(), "tick", (Styles.TipDelay + 2).ToString());
            if (screen.Scene.ShownTip != null)
            {
                opened++;
                Look(screen, $"{where}, the tip of \"{word.Text}\"", into);
            }

            screen.Do("move", "0", "0", "tick", "2");
        }

        return opened;
    }

    /// <summary>Every member each block offers as a task, chosen so its description shows, fits, in both languages.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("no")]
    public void EveryTaskMemberFits(string language)
    {
        var screen = Screen.WithGameFonts(Rig.Demo());
        Assert.True(screen.HasGameFonts, "the game's font was not found, so nothing was measured as the game measures it");
        screen.Editor.NewButtonSystem("Fit");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Run(2);
        if (language.Length > 0)
        {
            screen.Scene.Speak(language);
            screen.Run(2);
        }

        var found = new List<string>();
        var seen = 0;
        foreach (var block in screen.Editor.Pick().Stages[0].Entries().Where(e => !e.IsHeading).Select(e => e.Text).ToList())
        {
            screen.Editor.PressBlock(block);
            screen.Run(2);
            foreach (var member in screen.Editor.Pick().Stages[1].Entries().Where(e => !e.IsHeading).Select(e => e.Text).ToList())
            {
                screen.Editor.PressChoice(member);
                screen.Run(24);
                seen++;
                Look(screen, $"{block} / {member}", found);
            }
        }

        Assert.True(seen > 40, $"only {seen} members were seen");
        Assert.True(found.Count == 0, string.Join("\n", found.Distinct()));
    }

    /// <summary>Every signal and value each block offers a when trigger, chosen so its description shows, fits, in both languages.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("no")]
    public void EveryWhenMemberFits(string language)
    {
        var screen = Screen.WithGameFonts(Rig.Demo());
        Assert.True(screen.HasGameFonts, "the game's font was not found, so nothing was measured as the game measures it");
        screen.Editor.NewSystem("Fit");
        screen.Run(2);
        screen.Editor.PressStart(SentenceText.WhenWord);
        screen.Run(2);
        if (language.Length > 0)
        {
            screen.Scene.Speak(language);
            screen.Run(2);
        }

        var found = new List<string>();
        var seen = 0;
        foreach (var block in screen.Editor.Pick().Stages[0].Entries().Where(e => !e.IsHeading).Select(e => e.Text).ToList())
        {
            screen.Editor.PressBlock(block);
            screen.Run(2);
            foreach (var member in screen.Editor.Pick().Stages[1].Entries().Where(e => !e.IsHeading).Select(e => e.Text).ToList())
            {
                screen.Editor.PressChoice(member);
                screen.Run(2);
                seen++;
                Look(screen, $"{block} / {member}", found);
            }
        }

        Assert.True(seen > 40, $"only {seen} members were seen");
        Assert.True(found.Count == 0, string.Join("\n", found.Distinct()));
    }

    /// <summary><paramref name="presses" /> with each pressed word said in Norwegian, where the texts say it.</summary>
    static string[] Norwegian(string[] presses)
    {
        var english = Texts("MyTexts.resx");
        var norwegian = Texts("MyTexts.no.resx");
        var said = (string[])presses.Clone();
        for (var i = 0; i + 1 < said.Length; i++)
        {
            if (said[i] != "press" || said[i + 1].StartsWith("="))
                continue;

            // A label can be a mark joined to a text, "+ logic system" or "Terminal ›": the words are translated and the mark kept.
            var word = said[i + 1];
            var before = word.StartsWith("+ ") ? "+ " : "";
            var after = word.EndsWith(" ›") ? " ›" : "";
            var bare = word.Substring(before.Length, word.Length - before.Length - after.Length);
            var key = english.FirstOrDefault(text => text.Value == bare).Key;
            if (key != null && norwegian.TryGetValue(key, out var spoken))
                said[i + 1] = before + spoken + after;
        }

        return said.Select(Literal).ToArray();
    }

    /// <summary><paramref name="word" /> as pressed: a word the tour marks with = is the same in both languages.</summary>
    static string Literal(string word) => word.StartsWith("=") ? word.Substring(1) : word;

    static Dictionary<string, string> Texts(string file)
    {
        var path = Path.Combine(ResxTexts.Beside, file);
        return System.Xml.Linq.XDocument.Load(path).Root!.Elements("data")
            .GroupBy(data => (string)data.Attribute("name")!)
            .ToDictionary(group => group.Key, group => (string)group.First().Element("value")!);
    }

    static void Look(Screen screen, string where, List<string> into)
    {
        foreach (var said in screen.Cut.Concat(screen.Overlapping))
            into.Add($"{where}: {said}");
    }
}
