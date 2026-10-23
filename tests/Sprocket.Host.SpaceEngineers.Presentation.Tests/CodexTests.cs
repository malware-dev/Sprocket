using System.IO;
using System.Xml.Linq;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Sim.Content;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The codex: every tip a screen shows, read from Codex.resx, shown in the codex tip, its pages found by the names links give them.</summary>
public class CodexTests
{
    /// <summary>Resting the pointer on something with a tip for the delay opens the codex tip beside it, at what it says.</summary>
    [Fact]
    public void RestingOnAnElementOpensItsCodexTip()
    {
        var screen = Screen.Fresh("tick", "3");
        var at = screen.Scene.Find(screen.Editor.Texts.SystemsNew);
        var button = screen.Scene.Under(at)!;

        screen.Do("move", ((int)at.X).ToString(), ((int)at.Y).ToString(), "tick", (EditorView.TipDelay - 1).ToString());
        Assert.False(screen.Scene.CodexTip.IsOpen, "the tip opened before the pointer had rested for the delay");

        screen.Do("tick", "1");
        var tip = screen.Scene.CodexTip;
        Assert.True(tip.IsOpen, "no tip opened");
        Assert.Same(button, tip.Anchor);
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

    /// <summary>The editor's tips open a link's page in a tip beside them, casting the short shadow every hanging thing casts.</summary>
    [Fact]
    public void TheEditorsTipsNestAndCastAShadow()
    {
        var tip = Screen.Fresh("tick", "3").Scene.CodexTip;

        Assert.True(tip.Nests);
        Assert.Same(Styles.ShortShadow, tip.Shadow);
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

        screen.Do("move", ((int)at.X + 4).ToString(), ((int)at.Y + 6).ToString(), "tick", (EditorView.TipDelay * 2).ToString());
        Assert.False(screen.Scene.CodexTip.IsOpen, "a tip opened with tips off");

        screen.Do(((int)mark.X).ToString(), ((int)mark.Y).ToString(), "tick", "3");
        Assert.True(editor.TipsShown);
        Assert.Equal(new[] { false, true }, kept);
        Assert.NotNull(term.TipAt(term.FromScreen(heading + new Vector2(4f, 6f))));
    }

    /// <summary>A press beside an open tip closes it and still reaches what it lands on.</summary>
    [Fact]
    public void APressBesideTheTipClosesItAndGoesOn()
    {
        var screen = Screen.Fresh("tick", "3");
        var at = screen.Scene.Find(screen.Editor.Texts.SystemsNew);
        screen.Do("move", ((int)at.X + 4).ToString(), ((int)at.Y + 6).ToString(), "tick", EditorView.TipDelay.ToString());
        Assert.True(screen.Scene.CodexTip.IsOpen);

        screen.Do(((int)at.X).ToString(), ((int)at.Y).ToString(), "tick", "3");

        Assert.False(screen.Scene.CodexTip.IsOpen, "the press left the tip open");
        Assert.IsNotType<Features.Home.HomePage>(screen.Editor.Current);
    }

    /// <summary>The home screen's heading is the term logic system: resting on it shows the codex's page on it, and it takes no press.</summary>
    [Fact]
    public void TheHeadingExplainsLogicSystems()
    {
        var screen = Screen.Fresh("tick", "3");
        var at = FirstLink(screen, out var heading, CodexPages.LogicSystem);

        Assert.Same(screen.Editor.Codex.Find(CodexPages.LogicSystem), heading.TipAt(at));
        Assert.NotSame(heading, screen.Scene.Under(at));
    }

    /// <summary>The home screen's heading is as wide as its words, so its tip opens to the right of them, not under a heading stretched across the row.</summary>
    [Fact]
    public void TheHeadingsTipOpensRightOfItsWords()
    {
        var screen = Screen.Fresh("tick", "3");
        var at = FirstLink(screen, out var heading, CodexPages.LogicSystem);

        screen.Do("move", ((int)at.X + 4).ToString(), ((int)at.Y + 6).ToString(), "tick", EditorView.TipDelay.ToString());

        var tip = screen.Scene.CodexTip;
        Assert.Same(heading, tip.Anchor);
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
        editor.PressAdd(Features.Sentences.SentenceText.SetStateWords);
        screen.Run(1);
        editor.Pick().Stages[0].Entries().First(entry => entry.Text == "where").Pick(Features.Picking.HeldModifier.Alone, 1);
        screen.Run(3);

        var field = Target.Within(editor.Current!.View!).OfType<Features.Fields.WordsField>().First();
        var words = field.GetVisualChild(0);
        Assert.True(field.Bounds.Width > words.Bounds.Width, "the field does not fill more room than its words");
        Assert.Null(field.Tip);

        var view = editor.Current.View!;
        var onWords = words.ToScreen(new Vector2(words.Bounds.X + words.Bounds.Width / 2f, words.Bounds.Y + words.Bounds.Height / 2f));
        var beside = field.ToScreen(new Vector2(field.Bounds.X + 2f, words.Bounds.Y + words.Bounds.Height / 2f));
        Assert.NotNull(view.SayingAt(onWords));
        Assert.Null(view.SayingAt(beside));
    }

    /// <summary>A logic system's installations heading is the term installation: resting on it shows the codex's page on it, and it takes no press.</summary>
    [Fact]
    public void TheInstallationsHeadingExplainsInstallations()
    {
        var screen = Screen.Fresh("tick", "3");
        screen.Editor.NewButtonSystem("Airlock");
        screen.Editor.GoToRoot();
        screen.Do("tick", "3");
        var at = FirstLink(screen, out var heading, CodexPages.Installation);

        Assert.Same(screen.Editor.Codex.Find(CodexPages.Installation), heading.TipAt(at));
        Assert.NotSame(heading, screen.Scene.Under(at));
    }

    /// <summary>A heading holding a term is in capitals with the term alone a link, wherever in the heading its slot stands.</summary>
    [Theory]
    [InlineData("On your {constructs}", "ON YOUR [CONSTRUCTS](Term.Construct)")]
    [InlineData("{constructs} of yours", "[CONSTRUCTS](Term.Construct) OF YOURS")]
    [InlineData("All {constructs} here", "ALL [CONSTRUCTS](Term.Construct) HERE")]
    public void AHeadingLinksOnlyItsTerm(string template, string markdown)
    {
        Assert.Equal(markdown, CodexPages.Within(template, "constructs", "constructs", CodexPages.Construct));
    }

    /// <summary>A heading whose text lacks the term's slot is its words in capitals with no link.</summary>
    [Fact]
    public void AHeadingWithoutItsSlotIsItsWords()
    {
        Assert.Equal("ON YOUR SHIPS", CodexPages.Within("On your ships", "constructs", "constructs", CodexPages.Construct));
    }

    /// <summary>The library's first heading holds the term construct in every language: resting on it shows the codex's page on constructs.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("no")]
    public void TheLibrarysHeadingExplainsConstructs(string culture)
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Editor;
        if (culture.Length != 0)
        {
            screen.Scene.Speak(culture);
            editor.Reword();
        }

        editor.OpenLibrary();
        screen.Run(2);
        var at = FirstLink(screen, out var heading, CodexPages.Construct);

        Assert.Same(editor.Codex.Find(CodexPages.Construct), heading.TipAt(at));
    }

    /// <summary>The tip trial asking for Alt puts the line saying so under every tip, and holding Alt lets the pointer onto the tip.</summary>
    [Fact]
    public void TheAltTrialKeepsTipsByAlt()
    {
        var screen = Screen.Fresh("tick", "3");
        var tip = screen.Scene.CodexTip;
        Assert.False(tip.KeptByKey);

        screen.Scene.Ship.Settle(new Settings(SettingsLayer.Empty, SettingsLayer.Empty.WithTipKeeping(TipKeeping.Alt)));
        screen.Run(2);
        Assert.Equal(TipKeeping.Alt, screen.Editor.TipKeeping);
        Assert.True(tip.KeptByKey);

        var at = screen.Scene.Find(screen.Editor.Texts.SystemsNew);
        screen.Do("move", ((int)at.X).ToString(), ((int)at.Y).ToString(), "tick", EditorView.TipDelay.ToString());
        Assert.True(screen.Shows(screen.Editor.Texts.Codex.TipKeepHint), "the tip does not say how to keep it");

        screen.Scene.IsKeepKeyHeld = true;
        screen.Run(1);
        Assert.True(tip.IsKept);
    }

    /// <summary>
    /// A press on a tip hanging outside the menu its element is in reaches the tip and leaves the menu open; the tip closes with
    /// the menu.
    /// </summary>
    [Fact]
    public void APressOnATipOutsideItsMenuKeepsTheMenu()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Editor;
        editor.NewSystem("Airlock");
        screen.Run(2);

        var plus = Target.Within(editor.Current!.View!).OfType<PlusMenu>().First(menu => menu.IsVisible && menu.Choices.Count(c => c.Adds != null) > 1);
        Press(screen, Middle(plus));
        var menu = editor.Windows!.Top!;
        var row = Target.Within(menu).OfType<Lumel.Layout.HStack>().First(stack => stack.Tip != null);

        var at = Middle(row);
        screen.Do("move", Say(at.X), Say(at.Y), "tick", EditorView.TipDelay.ToString());
        var tip = screen.Scene.CodexTip;
        Assert.Same(row, tip.Anchor);
        var first = tip.Page;

        tip.Go(new Lumel.Tip("Another page."));
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

    /// <summary>The wheel turned over an open tip scrolls nothing: the page beneath stays where it is.</summary>
    [Fact]
    public void TheWheelOverATipScrollsNothing()
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
        screen.Do("move", Say(spot.X), Say(spot.Y), "tick", EditorView.TipDelay.ToString());
        var tip = screen.Scene.CodexTip;
        Assert.True(tip.IsOpen, "no tip opened over the first logic system");
        var onTip = new Vector2(tip.Bounds.X + tip.Bounds.Width / 2f, tip.Bounds.Y + tip.Bounds.Height / 2f);

        var before = screen.Text("Airlock 0").At.Y;
        screen.Scene.ScrollBy(onTip, 200f);
        screen.Run(1);
        Assert.Equal(before, screen.Text("Airlock 0").At.Y);
        Assert.Equal(heading.At.Y, screen.Text("LOGIC SYSTEMS").At.Y);

        // Beside the tip, the same wheel scrolls the page, so the page is one that scrolls.
        screen.Scene.ScrollBy(new Vector2(onTip.X, 900f), 200f);
        screen.Run(1);
        Assert.NotEqual(before, screen.Text("Airlock 0").At.Y);
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

    /// <summary>Every attribute a page of the codex asks its boxes for, in every language, is markdown-lite's own or one the codex gives a look.</summary>
    [Fact]
    public void EveryBoxInTheCodexAsksForSomethingItHas()
    {
        var native = new[] { "left", "center", "right" };
        foreach (var (file, key, text) in Pages())
        {
            foreach (var name in Names(Markdown.Parse(text).Blocks))
            {
                Assert.True(native.Contains(name, StringComparer.OrdinalIgnoreCase) || Styles.Boxes.Find(name) != null,
                    file + " " + key + " asks for " + name + ", which nothing gives a look");
            }
        }
    }

    /// <summary>Every page of every Codex file the mod ships: the file, the page's key, and its text.</summary>
    static IEnumerable<(string File, string Key, string Text)> Pages()
    {
        var files = Directory.GetFiles(ResxTexts.Beside, "Codex*.resx");
        Assert.NotEmpty(files);
        foreach (var path in files)
        {
            foreach (var data in XDocument.Load(path).Root!.Elements("data"))
                yield return (Path.GetFileName(path), data.Attribute("name")!.Value, data.Element("value")!.Value);
        }
    }

    /// <summary>The first point on the screen where a link to <paramref name="target" /> sits, and the text it sits in.</summary>
    static Vector2 FirstLink(Screen screen, out RichText text, string target) => Links.First(screen.Scene.Editor.Current!.View, target, out text);

    /// <summary>Every attribute the boxes in <paramref name="blocks" /> name, inside quotes and other boxes too.</summary>
    static IEnumerable<string> Names(IList<Block> blocks) =>
        blocks.SelectMany(block => block.Names.Concat(Names(block.Blocks)));

    /// <summary>The target of every link in <paramref name="blocks" />, inside tables, quotes and boxes too.</summary>
    static IEnumerable<string> Targets(IList<Block> blocks) =>
        blocks.SelectMany(block => block.Runs.OfType<LinkRun>().Select(run => run.Link.Target).Concat(Targets(block.Blocks)));
}
