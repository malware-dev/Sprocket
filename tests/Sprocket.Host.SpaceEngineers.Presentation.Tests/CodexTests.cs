using Sprocket.Host.SpaceEngineers.Features.Library;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using System.IO;
using System.Xml.Linq;
using Lumel.Text;
using Sprocket.Sim.Content;
using Sprocket.Sim.Machine;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The codex: every tip a screen shows, read from MyTextsCodex.resx, shown in the codex tip, its pages found by the names links give them.</summary>
public class CodexTests
{
    /// <summary>Resting the pointer on something with a tip for the delay opens the codex tip beside it, at what it says.</summary>
    [Fact]
    public void RestingOnAnElementOpensItsCodexTip()
    {
        var screen = Screen.Fresh("tick", "3");
        var at = screen.Scene.Find(screen.Editor.Texts.SystemsNew);
        var button = screen.Scene.Under(at)!;

        screen.Do("move", ((int)at.X).ToString(), ((int)at.Y).ToString(), "tick", (Styles.TipDelay - 1).ToString());
        Assert.Null(screen.Scene.ShownTip);

        screen.Do("tick", "1");
        var tip = screen.Scene.CodexTip;
        Assert.NotNull(tip);
        Assert.Same(button, tip.Owner);
        Assert.Equal(screen.Editor.Texts.Codex.SystemsNewAbout, tip.Page!.Text);
    }

    /// <summary>A page read on its own, the guide to rich text and the paused screen, is set in the game's own face, as every other page is.</summary>
    [Fact]
    public void APageReadOnItsOwnIsInTheGamesFace()
    {
        var page = new RichText { Style = Styles.Reading };
        var description = new RichText { Style = Styles.Description };

        Assert.Equal(description.Font, page.Font);
        Assert.Equal(description.StrongFont, page.StrongFont);
        Assert.Equal(description.EmphasisFont, page.EmphasisFont);
    }

    /// <summary>A tip holds the pointer and lights its terms only while the pin key is held, in the same tip.</summary>
    [Fact]
    public void ATipIsPinnedOnlyWhileItsKeyIsHeld()
    {
        var screen = Screen.Fresh("tick", "3");
        var at = screen.Scene.Find(screen.Editor.Texts.SystemsNew);
        screen.Do("move", ((int)at.X).ToString(), ((int)at.Y).ToString(), "tick", Styles.TipDelay.ToString());
        var tip = screen.Scene.CodexTip!;
        Assert.True(screen.Scene.View.Codex.IsPinnedByKey);
        Assert.False(tip.IsPinned);

        var held = true;
        // ReSharper disable once AccessToModifiedClosure - the key is let go further down.
        screen.Scene.View.Codex.FnIsPinKeyHeld = () => held;
        screen.Run(2);
        Assert.Same(tip, screen.Scene.CodexTip);
        Assert.True(tip.IsPinned);

        held = false;
        screen.Run(2);
        Assert.False(tip.IsPinned);
    }

    /// <summary>
    /// The question mark turns every tip off, and the look of terms with them, keeping the choice; pressed again it turns
    /// them back on.
    /// </summary>
    [Fact]
    public void TheQuestionMarkTurnsTipsOffAndOn()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Editor;
        var kept = new List<bool>();
        editor.KeepTips = kept.Add;
        var at = screen.Scene.Find(editor.Texts.SystemsNew);
        var heading = FirstLink(screen, out var term, CodexPages.LogicSystem);

        var mark = screen.Scene.Find("?");
        screen.Do(((int)mark.X).ToString(), ((int)mark.Y).ToString(), "tick", "3");
        Assert.False(editor.TipsShown);
        Assert.Equal(new[] { false }, kept);
        Assert.Null(term.TipAt(term.FromScreen(heading + new Vector2(4f, 6f))));
        Assert.Equal(Palette.Faint, term.LinkColor);

        screen.Do("move", ((int)at.X + 4).ToString(), ((int)at.Y + 6).ToString(), "tick", (Styles.TipDelay * 2).ToString());
        Assert.Null(screen.Scene.ShownTip);

        screen.Do(((int)mark.X).ToString(), ((int)mark.Y).ToString(), "tick", "3");
        Assert.True(editor.TipsShown);
        Assert.Equal(new[] { false, true }, kept);
        Assert.NotNull(term.TipAt(term.FromScreen(heading + new Vector2(4f, 6f))));
        Assert.Equal(Palette.Link, term.LinkColor);
    }

    /// <summary>Under the pointer, the tips toggle shows at once whether tips are on or off, and with tips off its tip still says the question mark turns them back on.</summary>
    [Fact]
    public void TheTipsToggleTellsOnFromOffAndKeepsItsTip()
    {
        var screen = Screen.Fresh("tick", "3");
        var mark = screen.Scene.Find("?");
        var x = ((int)mark.X).ToString();
        var y = ((int)mark.Y).ToString();
        screen.Do("move", x, y, "tick", "3");
        var on = screen.Text("?").Ink;

        screen.Do(x, y, "tick", "3");
        Assert.False(screen.Editor.TipsShown);

        Assert.NotEqual(on, screen.Text("?").Ink);

        screen.Do("move", x, y, "tick", (Styles.TipDelay * 2).ToString());

        var shown = screen.Scene.ShownTip;
        Assert.NotNull(shown);
        Assert.Equal(screen.Editor.Texts.Codex.ChromeTipsOn, shown.Text);
    }

    /// <summary>A kept tip's plain version gives each link as its words alone, and keeps its pictures and escapes.</summary>
    [Fact]
    public void AKeptTipsPlainVersionHasNoLinks()
    {
        var kept = new KeptTip("Keep a ![](mark:blueprint)&nbsp;[blueprint](Term.Blueprint), and [a \\] b](Term.Other).");

        Assert.Equal("Keep a ![](mark:blueprint)&nbsp;blueprint, and a \\] b.", kept.Plain.Text);
    }

    /// <summary>With tips off, a kept tip shows its plain version, so no link in it opens a tip of its own.</summary>
    [Fact]
    public void AKeptTipShowsNoLinksWithTipsOff()
    {
        var screen = Screen.Fresh("press \"+ logic system\" type Airlock enter".Split(' '));
        var editor = screen.Editor;
        editor.GoToRoot();
        editor.OpenLibrary();
        screen.Run(2);
        var page = Assert.IsType<LibraryPage>(editor.Current);
        page.Rows.OfType<StatusRow>().First(row => row.Summary == "Airlock").Opens!.Execute();
        screen.Run(2);

        var mark = screen.Scene.Find("?");
        screen.Do(((int)mark.X).ToString(), ((int)mark.Y).ToString(), "tick", "3");
        Assert.False(editor.TipsShown);

        var arrow = page.Moves.ToLibrary.Bounds;
        screen.Do("move", ((int)(arrow.X + arrow.Width / 2)).ToString(), ((int)(arrow.Y + arrow.Height / 2)).ToString(),
            "tick", (Styles.TipDelay * 2).ToString());

        var shown = screen.Scene.ShownTip;
        Assert.NotNull(shown);
        Assert.Equal(((KeptTip)page.Moves.ToLibrary.Tip!).Plain.Text, shown.Text);
        Assert.DoesNotContain("(Term.", shown.Text);
    }

    /// <summary>A press beside an open tip closes it and still reaches what it lands on.</summary>
    [Fact]
    public void APressBesideTheTipClosesItAndGoesOn()
    {
        var screen = Screen.Fresh("tick", "3");
        var at = screen.Scene.Find(screen.Editor.Texts.SystemsNew);
        screen.Do("move", ((int)at.X + 4).ToString(), ((int)at.Y + 6).ToString(), "tick", Styles.TipDelay.ToString());
        Assert.NotNull(screen.Scene.CodexTip);

        screen.Do(((int)at.X).ToString(), ((int)at.Y).ToString(), "tick", "3");

        Assert.Null(screen.Scene.ShownTip);
        Assert.IsNotType<Features.Home.HomePage>(screen.Editor.Current);
    }

    /// <summary>The home screen's heading is as wide as its words, so its tip opens to the right of them, not under a heading stretched across the row.</summary>
    [Fact]
    public void TheHeadingsTipOpensRightOfItsWords()
    {
        var screen = Screen.Fresh("tick", "3");
        var at = FirstLink(screen, out var heading, CodexPages.LogicSystem);

        screen.Do("move", ((int)at.X + 4).ToString(), ((int)at.Y + 6).ToString(), "tick", Styles.TipDelay.ToString());

        var tip = screen.Scene.CodexTip!;
        Assert.Same(heading, tip.Owner);
        Assert.Equal(Lumel.TipSide.Right, tip.Side);
        Assert.Equal(heading.Bounds.X + heading.Bounds.Width, tip.Bounds.X, 1);
        Assert.True(heading.Bounds.Width <= heading.DesiredSize.X + 1f, "the heading is wider than its words");
    }

    /// <summary>
    /// A field of words fills its room, but its tip sits on the words it shows: resting on them says it, and resting on the
    /// empty room beside them says nothing.
    /// </summary>
    [Fact]
    public void AWordsFieldsTipIsOnItsWords()
    {
        const string program = "sprocket 1 alpha 18\n\nsystem \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock\n{\n    type side = inside | outside\n\n"
            + "    let where = inside\n\n    sequence 1 {\n        set where to outside\n    }\n}\n";
        var screen = Screen.Holding(program);
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        system.Enabled = false;
        editor.Program.Edited();
        editor.Push(new Features.Sequences.SequencePage(editor, system.Sequences[0]));
        screen.Run(1);
        editor.PressAdd(Framework.Ui.Parts.SentenceText.SetStateWords);
        screen.Run(1);
        editor.Pick().Stages[0].Entries().First(entry => entry.Text == "where").Pick(HeldModifier.Alone, 1);
        screen.Run(3);

        var field = Target.Within(editor.Current!.View!).OfType<Framework.Ui.Fields.WordsField>().First();
        var words = field.GetVisualChild(0);
        Assert.True(field.Bounds.Width > words.Bounds.Width, "the field does not fill more room than its words");
        Assert.Null(field.Tip);

        var view = editor.Current.View!;
        var onWords = words.ToScreen(new Vector2(words.Bounds.X + words.Bounds.Width / 2f, words.Bounds.Y + words.Bounds.Height / 2f));
        var beside = field.ToScreen(new Vector2(field.Bounds.X + 2f, words.Bounds.Y + words.Bounds.Height / 2f));
        Assert.NotNull(view.TipOwnerAt(onWords));
        Assert.Null(view.TipOwnerAt(beside));
    }

    /// <summary>A heading holding a term is in capitals with the term alone a link, wherever in the heading its slot stands, and a heading whose text lacks the slot is its words in capitals with no link.</summary>
    [Theory]
    [InlineData("On your {constructs}", "ON YOUR [CONSTRUCTS](Term.Construct class=term)")]
    [InlineData("{constructs} of yours", "[CONSTRUCTS](Term.Construct class=term) OF YOURS")]
    [InlineData("All {constructs} here", "ALL [CONSTRUCTS](Term.Construct class=term) HERE")]
    [InlineData("On your ships", "ON YOUR SHIPS")]
    public void AHeadingLinksOnlyItsTerm(string template, string markdown)
    {
        Assert.Equal(markdown, CodexPages.Within(template, "constructs", "constructs", CodexPages.Construct));
    }

    /// <summary>
    /// A press on a tip hanging outside the menu its element is in reaches the tip and leaves the menu open; the tip closes with
    /// the menu.
    /// </summary>
    [Fact]
    public void APressOnATipOutsideItsMenuKeepsTheMenu()
    {
        var screen = Screen.Fresh("tick", "3");
        screen.Scene.View.Codex.FnIsPinKeyHeld = () => true;
        var editor = screen.Editor;
        editor.NewSystem("Airlock");
        screen.Run(2);

        var plus = Target.Within(editor.Current!.View!).OfType<PlusMenu>().First(menu => menu.IsVisible && menu.Choices.Count(c => c.Adds != null) > 1);
        Press(screen, Middle(plus));
        var menu = editor.Windows!.Top!;
        var row = Target.Within(menu).OfType<Lumel.Menus.MenuItem>().First(item => item.Tip != null);

        var at = Middle(row);
        screen.Do("move", Say(at.X), Say(at.Y), "tick", Styles.TipDelay.ToString());
        var tip = screen.Scene.CodexTip!;
        Assert.Same(row, tip.Owner);
        var first = tip.Page;

        tip.Navigate(new Lumel.Tip("Another page."));
        screen.Run(1);
        var back = Target.Within(tip).OfType<Lumel.Images.Glyph>().First(glyph => glyph.IsVisible);
        Assert.False(menu.Bounds.Contains(Middle(back)), "the tip's back step is inside the menu");
        Press(screen, Middle(back));

        Assert.True(editor.Windows.IsOpen(menu), "the press on the tip closed the menu");
        Assert.Same(first, tip.Page);

        editor.Windows.Close(menu);
        screen.Run(1);
        Assert.False(tip.IsOpen, "the tip stayed after its element's menu closed");
    }

    /// <summary>The wheel turned over an open tip scrolls the page beneath it.</summary>
    [Fact]
    public void TheWheelOverATipScrollsThePage()
    {
        var screen = Screen.Fresh("tick", "3");
        for (var i = 0; i < 16; i++)
        {
            screen.Editor.NewSystem("Airlock " + i);
            screen.Editor.GoToRoot();
        }

        screen.Run(3);
        var heading = screen.Text("LOGIC SYSTEMS");
        var name = screen.Text("Airlock 0");
        var over = new Vector2(name.At.X, name.At.Y);
        var spot = Screen.Ui(over.X + 4f, over.Y + 6f);
        screen.Do("move", Say(spot.X), Say(spot.Y), "tick", Styles.TipDelay.ToString());
        var tip = screen.Scene.CodexTip;
        Assert.NotNull(tip);
        Assert.True(tip.IsOpen, "no tip opened over the first logic system");
        var onTip = new Vector2(tip.Bounds.X + tip.Bounds.Width / 2f, tip.Bounds.Y + tip.Bounds.Height / 2f);

        var before = screen.Text("Airlock 0").At.Y;
        screen.Scene.ScrollBy(onTip, 200f);
        screen.Run(1);
        Assert.True(screen.Text("Airlock 0").At.Y < before, "the page stayed put under the tip");
        Assert.True(screen.Text("LOGIC SYSTEMS").At.Y < heading.At.Y, "the heading stayed put under the tip");
    }

    /// <summary>The Markup button opens the guide to rich text, which is read again in the language the editor comes to speak.</summary>
    [Fact]
    public void TheGuideOpensAndIsReadAgainInANewLanguage()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Editor;

        editor.Guides.Execute();
        screen.Run(2);
        Assert.True(editor.Windows!.IsOpen(editor.Guide!));
        Assert.True(screen.Shows("Rich text"), "the guide is not titled with its page's heading");

        screen.Scene.Speak("no");
        editor.Reword();
        screen.Run(2);
        Assert.True(screen.Shows("Formatert tekst"), "the guide was not read again in Norwegian");
    }

    static Vector2 Middle(Lumel.Element element) =>
        element.ToScreen(new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f));

    static void Press(Screen screen, Vector2 at) => screen.Do(Say(at.X), Say(at.Y), "tick", "2");

    static string Say(float value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>A page is found by the name a link gives it, the same page each time, and a name the codex has no page for finds nothing.</summary>
    [Fact]
    public void APageIsFoundByItsLinkName()
    {
        var pages = Simulation.Fresh().Editor.Codex;

        var page = pages.Find(CodexPages.LogicSystem);
        Assert.NotNull(page);
        Assert.Same(page, pages.Find(CodexPages.LogicSystem));
        Assert.NotNull(pages.Find(CodexPages.Installation));
        Assert.Null(pages.Find("Term.Nowhere"));
    }

    /// <summary>The codex is read in the language the editor speaks, and read again once the editor comes to speak another.</summary>
    [Fact]
    public void TheCodexIsReadInTheEditorsLanguage()
    {
        var screen = Screen.Fresh("tick", "3");
        var english = screen.Editor.Codex.Find(CodexPages.LogicSystem)!.Text;

        screen.Scene.Speak("no");
        screen.Editor.Reword();

        var norwegian = screen.Editor.Codex.Find(CodexPages.LogicSystem)!.Text;
        Assert.Equal(screen.Editor.Texts.Codex.TermLogicSystem, norwegian);
        Assert.NotEqual(english, norwegian);
    }

    /// <summary>Every link in every page of the codex, in every language, names a page the codex has, so no page leads nowhere.</summary>
    [Fact]
    public void EveryLinkInTheCodexLeadsToAPage()
    {
        var pages = Simulation.Fresh().Editor.Codex;
        foreach (var (file, key, text) in Pages())
        {
            foreach (var target in Targets(Markdown.Parse(text).Blocks))
                Assert.True(pages.Find(target) != null, file + " " + key + " links to " + target + ", which the codex lacks");
        }
    }

    /// <summary>Every kind icon a page of the codex asks for, in every language, is a mark the editor can draw.</summary>
    [Fact]
    public void EveryIconInTheCodexIsAMark()
    {
        var marks = new MarkImages(null);
        foreach (var (file, key, text) in Pages())
        {
            for (var at = text.IndexOf("](mark:", StringComparison.Ordinal); at >= 0; at = text.IndexOf("](mark:", at + 1, StringComparison.Ordinal))
            {
                var end = text.IndexOf(')', at);
                var locator = text.Substring(at + 2, end - at - 2);
                Assert.True(marks.Find(locator) != null, file + " " + key + " asks for " + locator + ", which is no mark");
            }
        }
    }

    /// <summary>Every class a page of the codex asks its callouts for, in every language, is markdown-lite's own or one the codex gives a look.</summary>
    [Fact]
    public void EveryBoxInTheCodexAsksForSomethingItHas()
    {
        var native = new[] { "left", "center", "right" };
        var callouts = ((RichTextStyle)Styles.Description).Base.Callouts;
        foreach (var (file, key, text) in Pages())
        {
            foreach (var name in Names(Markdown.Parse(text).Blocks))
            {
                Assert.True(native.Contains(name, StringComparer.OrdinalIgnoreCase) || callouts.Find(name) != null,
                    file + " " + key + " asks for " + name + ", which nothing gives a look");
            }
        }
    }

    /// <summary>Every page of every Codex file the mod ships: the file, the page's key, and its text.</summary>
    static IEnumerable<(string File, string Key, string Text)> Pages()
    {
        var files = Directory.GetFiles(ResxTexts.Beside, "MyTextsCodex*.resx");
        Assert.NotEmpty(files);
        foreach (var path in files)
        {
            foreach (var data in XDocument.Load(path).Root!.Elements("data"))
                yield return (Path.GetFileName(path), data.Attribute("name")!.Value, data.Element("value")!.Value);
        }
    }

    /// <summary>The first point on the screen where a link to <paramref name="target" /> sits, and the text it sits in.</summary>
    static Vector2 FirstLink(Screen screen, out RichText text, string target) => Links.First(screen.Scene.Editor.Current!.View, target, out text);

    /// <summary>Every class the callouts in <paramref name="blocks" /> name, inside quotes and other callouts too.</summary>
    static IEnumerable<string> Names(IReadOnlyList<Block> blocks) =>
        blocks.SelectMany(block => block.Classes.Concat(Names(block.Blocks)));

    /// <summary>The target of every link in <paramref name="blocks" />, inside tables, quotes and boxes too.</summary>
    static IEnumerable<string> Targets(IReadOnlyList<Block> blocks) =>
        blocks.SelectMany(block => block.Runs.OfType<LinkRun>().Select(run => run.Link.Target).Concat(Targets(block.Blocks)));
}
