using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The words a step opens with that are a term in the codex, if, otherwise if, otherwise, delay, run and stop, have the term's
/// look and its page as their tip on the first line of a sequence's screen that leads to that page, and are plain words on every
/// later one.
/// </summary>
public class StepTermTests
{
    /// <summary>
    /// Each page a step's opening words are the term of is linked from the first row opening with any of its words, and from no
    /// row after it: <paramref name="first" /> is the kind of that row, <paramref name="kinds" /> every kind whose words lead there.
    /// </summary>
    [Theory]
    [InlineData(CodexPages.If, StepKind.If, new[] { StepKind.If })]
    [InlineData(CodexPages.Branch, StepKind.OtherwiseIf, new[] { StepKind.OtherwiseIf, StepKind.Otherwise })]
    [InlineData(CodexPages.Delay, StepKind.Wait, new[] { StepKind.Wait })]
    [InlineData(CodexPages.RunAndStop, StepKind.Run, new[] { StepKind.Run, StepKind.Stop })]
    public void EachPageIsLinkedOnlyFromItsFirstRow(string page, StepKind first, StepKind[] kinds)
    {
        var (screen, rows) = Shown(Twice);

        var those = rows.Where(row => kinds.Contains(row.Kind)).ToList();
        Assert.True(those.Count >= 2, "the program has too few rows of the page's words");
        Assert.Equal(first, those[0].Kind);
        Assert.Equal(page, those[0].LeadTerm);
        Assert.True(Links.Has(those[0], page), "the first " + SentenceText.Lead(first) + " does not link its page");
        Assert.All(those.Skip(1), row =>
        {
            Assert.Null(row.LeadTerm);
            Assert.False(Links.Has(row, page), "a later " + SentenceText.Lead(row.Kind) + " links the page too");
        });

        var at = Links.First(those[0], page, out var words);
        Assert.Same(screen.Editor.Codex.Find(page), words.TipAt(at));
    }

    /// <summary>A paused row's linked opening word is as quiet as the rest of its sentence.</summary>
    [Fact]
    public void APausedRowsTermIsDimmedWithItsSentence()
    {
        var (screen, rows) = Shown(Twice);
        rows.First(row => row.Kind == StepKind.Wait).Pauses!.Execute();
        screen.Run(3);

        var paused = screen.Editor.StepRows().First(row => row.Kind == StepKind.Wait);
        Assert.True(paused.IsOff);
        Assert.True(Links.Has(paused, CodexPages.Delay));
        var words = Target.Within(paused).OfType<Lumel.Text.RichText>().First();
        Assert.Equal(Palette.Dim, words.Color);
        Assert.Equal(Palette.Dim, words.LinkColor);
    }

    /// <summary>Resting the pointer on the first if opens the codex tip at the page on ifs.</summary>
    [Fact]
    public void RestingOnTheFirstIfShowsItsPage()
    {
        var (screen, rows) = Shown(Twice);
        var at = Links.First(rows.First(row => row.Kind == StepKind.If), CodexPages.If, out _);

        // The first point of a link is on its top left corner; a few units in is on its words.
        screen.Do("move", Say(at.X + 4f), Say(at.Y + 6f), "tick", EditorView.TipDelay.ToString());

        Assert.True(screen.Scene.CodexTip.IsOpen, "no tip opened on the if");
        Assert.Same(screen.Editor.Codex.Find(CodexPages.If), screen.Scene.CodexTip.Page);
    }

    /// <summary>Once the first of a word is taken away, the next one on the screen becomes its term.</summary>
    [Fact]
    public void TheNextOfAWordTakesOverWhenTheFirstGoes()
    {
        var (screen, rows) = Shown(Twice);
        var first = rows.First(row => row.Kind == StepKind.Wait);

        first.Deletes!.Execute();
        screen.Run(3);

        var delay = screen.Editor.StepRows().Single(row => row.Kind == StepKind.Wait);
        Assert.Equal(CodexPages.Delay, delay.LeadTerm);
        Assert.True(Links.Has(delay, CodexPages.Delay));
    }

    /// <summary>The words a step opens with are read once and kept while nothing changes, however many ticks pass.</summary>
    [Fact]
    public void TheOpeningWordsAreNotReadAgainOnEveryTick()
    {
        var (screen, rows) = Shown(Twice);
        var words = Target.Within(rows.First(row => row.Kind == StepKind.If)).OfType<Lumel.Text.RichText>().First();
        var read = words.Document;

        screen.Run(30);

        Assert.Same(read, words.Document);
    }

    /// <summary>A step that opens with words that are no term in the codex has plain words.</summary>
    [Fact]
    public void WordsThatAreNoTermStayPlain()
    {
        var (_, rows) = Shown(Twice);

        Assert.All(rows.Where(row => row.Kind == StepKind.Do || row.Kind == StepKind.End || row.Kind == StepKind.Adds),
            row => Assert.Null(row.LeadTerm));
    }

    /// <summary>The editor holding <paramref name="program" />, on the screen of its first sequence with its logic system off, and the rows of that screen.</summary>
    static (Screen Screen, List<StepRow> Rows) Shown(string program)
    {
        var screen = Screen.Holding(program);
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        system.Enabled = false;
        editor.Program.Edited();
        editor.Push(new SequencePage(editor, system.Sequences[0]));
        screen.Run(5);
        return (screen, editor.StepRows());
    }

    static string Say(float value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    // Two of every step that opens with a term: two ifs, each with an otherwise if and an otherwise, two delays, two runs and two stops.
    const string Twice = "sprocket 1 alpha 18\n\n"
        + "let door1 = block 'Door/LargeBlockDoor' [ 2 ]\n"
        + "let door2 = block 'Door/LargeBlockDoor' [ 3 ]\n"
        + "\n"
        + "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock\n{\n"
        + "    sequence 1 {\n"
        + "        call ext.close on [ door1 ]\n"
        + "        wait 60t\n"
        + "        run 2\n"
        + "        stop 2\n"
        + "        if all ext.status on [ door1 ] == closed then {\n"
        + "            call ext.open on [ door2 ]\n"
        + "        }\n"
        + "        else if all ext.status on [ door2 ] == closed then {\n"
        + "            call ext.open on [ door1 ]\n"
        + "        }\n"
        + "        else {\n"
        + "            call ext.close on [ door2 ]\n"
        + "        }\n"
        + "        wait 30t\n"
        + "        run 2\n"
        + "        stop 2\n"
        + "        if all ext.status on [ door2 ] == open then {\n"
        + "            call ext.close on [ door2 ]\n"
        + "        }\n"
        + "        else if all ext.status on [ door1 ] == open then {\n"
        + "            call ext.close on [ door1 ]\n"
        + "        }\n"
        + "        else {\n"
        + "            call ext.open on [ door2 ]\n"
        + "        }\n"
        + "    }\n"
        + "    sequence 2 {\n"
        + "        wait 10t\n"
        + "    }\n"
        + "}\n";
}
