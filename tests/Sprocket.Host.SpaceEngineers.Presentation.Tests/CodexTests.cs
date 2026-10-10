using System.IO;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.Home;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Sim.Content;
using Sprocket.Sim.Machine;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

public class CodexTests
{
    /// <summary>The question mark on the home screen opens the codex's window at the home screen's entry, beside the home screen, which stays.</summary>
    [Fact]
    public void TheQuestionMarkOpensTheScreensEntry()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Scene.Editor;

        editor.Screen.Help!.Execute();
        screen.Do("tick", "3");

        var codex = editor.CodexWindow!;
        Assert.True(editor.Windows!.IsOpen(codex));
        Assert.Equal(Terms.Home, codex.Term!.Id);
        Assert.False(codex.CanStepBack);
        Assert.False(codex.CanStepForward);
        Assert.IsType<HomePage>(editor.Current);
    }

    /// <summary>The question mark closes the codex where it is already open, and opens it at the screen's entry again after.</summary>
    [Fact]
    public void TheQuestionMarkClosesAnOpenCodex()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Scene.Editor;
        var terms = editor.Codex;

        editor.Screen.Help!.Execute();
        screen.Do("tick", "3");
        editor.CodexWindow!.Go(terms.Find(Terms.Sequence));

        editor.Screen.Help!.Execute();
        screen.Do("tick", "3");
        Assert.False(editor.CodexOpen, "the question mark did not close the codex");

        editor.Screen.Help!.Execute();
        screen.Do("tick", "3");
        Assert.True(editor.CodexOpen);
        Assert.Equal(Terms.Home, editor.CodexWindow!.Term!.Id);
    }

    /// <summary>The codex keeps a history of its own: moving on drops what was stepped back from, and closing it leaves the screen as it was.</summary>
    [Fact]
    public void TheCodexStepsThroughAHistoryOfItsOwn()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Scene.Editor;
        var terms = editor.Codex;
        editor.OpenCodex(terms.Find(Terms.LogicSystem)!);
        var codex = editor.CodexWindow!;

        codex.Go(terms.Find(Terms.Installation));
        codex.Go(terms.Find(Terms.Sequence));
        Assert.Equal(Terms.Sequence, codex.Term!.Id);

        codex.StepBack();
        codex.StepBack();
        Assert.Equal(Terms.LogicSystem, codex.Term!.Id);
        Assert.False(codex.CanStepBack);

        codex.StepForward();
        Assert.Equal(Terms.Installation, codex.Term!.Id);

        codex.Go(terms.Find(Terms.Library));
        Assert.False(codex.CanStepForward);

        codex.Close();
        screen.Do("tick", "3");
        Assert.False(editor.Windows!.IsOpen(codex));
        Assert.IsType<HomePage>(editor.Current);
    }

    /// <summary>The codex's contents show in place of the page, and choosing a term there moves to it as a step forward.</summary>
    [Fact]
    public void TheContentsListEveryTermAndMoveThere()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Scene.Editor;
        editor.OpenCodex(editor.Codex.Find(Terms.Installation)!);
        var codex = editor.CodexWindow!;

        codex.List(true);
        Assert.True(codex.IsListing);

        codex.Go(editor.Codex.Find(Terms.Library));
        Assert.False(codex.IsListing);
        Assert.Equal(Terms.Library, codex.Term!.Id);

        codex.StepBack();
        Assert.Equal(Terms.Installation, codex.Term!.Id);
    }

    /// <summary>On a screen with no room beside the editor, the codex first stands inside it, against its right edge.</summary>
    [Fact]
    public void TheCodexFirstStandsAgainstTheEditorsRightEdge()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Scene.Editor;
        editor.OpenCodex(editor.Codex.Find(Terms.Home)!);
        screen.Do("tick", "3");

        var window = editor.CodexWindow!.Bounds;
        var page = editor.Current.View!.Bounds;
        Assert.True(window.Width > 0f, "the codex was not laid out");
        Assert.InRange(window.X + window.Width - (page.X + page.Width), -40f, 40f);
        Assert.True(window.X > page.X, "the codex does not stand inside the editor");
    }

    /// <summary>Dragged by its title bar, the codex moves with the pointer, and is found where it was left when it opens again.</summary>
    [Fact]
    public void TheCodexIsMovedByItsTitleBarAndStaysWhereItWasLeft()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Scene.Editor;
        editor.OpenCodex(editor.Codex.Find(Terms.Home)!);
        screen.Do("tick", "3");

        var codex = editor.CodexWindow!;
        var before = codex.Bounds;
        var x = (int)(before.X + before.Width * 0.5f);
        var y = (int)(before.Y + 24f);
        screen.Do("drag", x.ToString(), y.ToString(), (x - 200).ToString(), (y + 50).ToString(), "tick", "3");

        Assert.Equal(before.X - 200f, codex.Bounds.X, 1f);
        Assert.Equal(before.Y + 50f, codex.Bounds.Y, 1f);
        Assert.Equal(new Vector2(before.X - 200f, before.Y + 50f), editor.Habits.CodexAt);

        codex.Close();
        editor.OpenCodex(editor.Codex.Find(Terms.Library)!);
        screen.Do("tick", "3");
        Assert.Equal(before.X - 200f, codex.Bounds.X, 1f);
    }

    /// <summary>
    /// The codex moves most of the way off the screen, as a desktop window does, and never all of it: some of it stays on
    /// each side, and its title bar never goes above the screen.
    /// </summary>
    [Fact]
    public void TheCodexMovesMostOfTheWayOffTheScreen()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Scene.Editor;
        editor.OpenCodex(editor.Codex.Find(Terms.Home)!);
        screen.Do("tick", "3");

        var codex = editor.CodexWindow!;
        var before = codex.Bounds;
        var x = (int)(before.X + before.Width * 0.5f);
        var y = (int)(before.Y + 24f);
        screen.Do("drag", x.ToString(), y.ToString(), (x + 5000).ToString(), (y + 5000).ToString(), "tick", "3");

        var room = codex.Screen;
        Assert.True(codex.Bounds.X > room.X + room.Width - before.Width, "the codex is held wholly on the screen");
        Assert.Equal(room.X + room.Width - codex.Kept, codex.Bounds.X, 1f);
        Assert.Equal(room.Y + room.Height - codex.Kept, codex.Bounds.Y, 1f);

        x = (int)(codex.Bounds.X + codex.Kept - 8f);
        y = (int)(codex.Bounds.Y + 24f);
        screen.Do("drag", x.ToString(), y.ToString(), (x - 9000).ToString(), (y - 9000).ToString(), "tick", "3");

        Assert.Equal(room.X - before.Width + codex.Kept, codex.Bounds.X, 1f);
        Assert.Equal(room.Y, codex.Bounds.Y, 1f);
    }

    /// <summary>The wheel turned over the codex's page scrolls the page, not the screen beneath it.</summary>
    [Fact]
    public void TheWheelOverTheCodexScrollsItsPage()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Scene.Editor;
        editor.OpenCodex(editor.Codex.Find(Terms.LogicSystem)!);
        screen.Do("tick", "3");

        var codex = editor.CodexWindow!;
        var page = Assert.Single(codex.Within().OfType<Lumel.Viewing.ScrollView>());
        Assert.True(page.CanScroll, "the page is short enough not to scroll");

        var at = codex.Bounds;
        var x = (int)(at.X + at.Width * 0.5f);
        var y = (int)(at.Y + at.Height * 0.5f);
        screen.Do("scroll", x.ToString(), y.ToString(), "300", "tick", "3");

        Assert.True(page.Scrolled > 0f, "the page did not move");
    }

    /// <summary>Nothing beneath the codex's window is reached through it: the pointer anywhere on it rests on the window and is kept from the screen under it.</summary>
    [Fact]
    public void NothingIsReachedThroughTheCodex()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Scene.Editor;
        editor.OpenCodex(editor.Codex.Find(Terms.LogicSystem)!);
        screen.Do("tick", "3");

        var codex = editor.CodexWindow!;
        for (var y = codex.Bounds.Y + 1f; y < codex.Bounds.Y + codex.Bounds.Height; y += 7f)
        {
            for (var x = codex.Bounds.X + 1f; x < codex.Bounds.X + codex.Bounds.Width; x += 7f)
            {
                var point = new Vector2(x, y);
                editor.Windows!.Under(point, out var stopped);
                Assert.True(stopped, "the pointer at " + x + ", " + y + " was not kept from the screen beneath the codex");

                var under = screen.Scene.Under(point);
                if (under != null)
                    Assert.True(Within(under, codex), "the pointer at " + x + ", " + y + " reached " + under.GetType().Name + " beneath the codex");
            }
        }
    }

    /// <summary>The home screen's heading is a link to the logic system's entry: resting on it shows the term's preview, and pressing it opens the codex there.</summary>
    [Fact]
    public void TheHeadingLeadsIntoTheCodex()
    {
        var screen = Screen.Fresh("tick", "3");
        var at = FirstLink(screen, out var heading);
        var term = screen.Scene.Editor.Codex.Find(Terms.LogicSystem)!;

        Assert.Same(term.Preview, heading.TipAt(at));

        screen.Do(((int)at.X).ToString(), ((int)at.Y).ToString(), "tick", "3");

        Assert.Equal(Terms.LogicSystem, screen.Scene.Editor.CodexWindow!.Term!.Id);
    }

    /// <summary>A link inside a codex page moves the codex on to its term, as a step forward in the codex's own history.</summary>
    [Fact]
    public void ALinkInAPageMovesTheCodexOn()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Scene.Editor;
        editor.OpenCodex(editor.Codex.Find(Terms.LogicSystem)!);
        screen.Do("tick", "3");
        var codex = editor.CodexWindow!;

        var at = FirstLink(screen, out var text, codex);
        var target = text.LinkAt(at)!.Target;
        screen.Do(((int)at.X).ToString(), ((int)at.Y).ToString(), "tick", "3");

        Assert.Equal(target, "#" + codex.Term!.Id);
        Assert.NotEqual(Terms.LogicSystem, codex.Term!.Id);
        Assert.True(codex.CanStepBack);
    }

    /// <summary>The codex lists the terms the shipped index lists, in its order, and finds nothing wrong with it.</summary>
    [Fact]
    public void TheCodexListsWhatItsIndexLists()
    {
        var sim = Simulation.Fresh();
        var index = File.ReadAllLines(Path.Combine(CodexFiles.Beside, "index"))
            .Select(line => line.Trim().TrimStart('>').Trim()).Where(line => line.Length > 0 && line[0] != '#');

        Assert.Equal(index, sim.Editor.Codex.All.Select(term => term.Id));
        Assert.Equal("", sim.Editor.Codex.Trouble);
    }

    /// <summary>The index decides which terms there are and their order, past its comments, blank lines and an id given twice.</summary>
    [Fact]
    public void TheIndexOrdersTheTerms()
    {
        var sim = Simulation.Fresh();
        sim.Texts.Book.Pages = new Pages("index", "# The terms.\nlibrary\n\nhome\r\nlibrary\n");

        var terms = new Terms(sim.Texts);

        Assert.Equal(new[] { Terms.Library, Terms.Home }, terms.All.Select(term => term.Id));
    }

    /// <summary>A line of the index starting with <c>&gt;</c> begins a section: its term heads it, and the terms after it are listed under it.</summary>
    [Fact]
    public void ALineStartingWithAnAngleBeginsASection()
    {
        var sim = Simulation.Fresh();
        sim.Texts.Book.Pages = new Pages("index", "home\n> library\npicking\n");

        var terms = new Terms(sim.Texts);

        Assert.Equal(new[] { Terms.Home, Terms.Library, Terms.Picking }, terms.All.Select(term => term.Id));
        Assert.Equal(new[] { false, true, false }, terms.All.Select(term => term.Opens));
    }

    /// <summary>The contents show a section's term as its heading, and the terms of the section in from it.</summary>
    [Fact]
    public void TheContentsShowASectionUnderItsHeading()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Scene.Editor;
        editor.Texts.Book.Pages = new Pages("index", "home\n> library\npicking\n",
            "home.md", "# Home\n", "library.md", "# Library\n", "picking.md", "# Picking\n");
        editor.OpenCodex(editor.Codex.Find(Terms.Home)!);
        var codex = editor.CodexWindow!;
        codex.List(true);

        var entries = codex.Within().OfType<Lumel.Controls.Button>().ToList();
        var heading = Assert.Single(entries, b => b.Text == "Library");
        var under = Assert.Single(entries, b => b.Text == "Picking");
        var before = Assert.Single(entries, b => b.Text == "Home");
        Assert.Same(Styles.CodexSection, heading.Style);
        Assert.Same(Styles.CodexEntry, under.Style);
        Assert.Equal(Styles.CodexIndent, ((Lumel.Layout.HStack)under.Parent!).Padding.Left);
        Assert.Equal(0f, ((Lumel.Layout.HStack)before.Parent!).Padding.Left);
    }

    /// <summary>A term a screen links to that the index leaves out is said, since that screen's help opens nothing.</summary>
    [Fact]
    public void ATermAScreenLinksToIsMissedWhenTheIndexLeavesItOut()
    {
        var sim = Simulation.Fresh();
        sim.Texts.Book.Pages = new Pages("index", "home\n");

        var terms = new Terms(sim.Texts);

        Assert.Null(terms.Find(Terms.Library));
        Assert.Contains("the index leaves out library", terms.Trouble);
        Assert.DoesNotContain("leaves out home", terms.Trouble);
    }

    /// <summary>A codex with no index has no terms, and says so.</summary>
    [Fact]
    public void ACodexWithNoIndexSaysSo()
    {
        var sim = Simulation.Fresh();
        sim.Texts.Book.Pages = new Pages();

        var terms = new Terms(sim.Texts);

        Assert.Empty(terms.All);
        Assert.StartsWith("the codex has no index", terms.Trouble);
    }

    /// <summary>Every term the codex lists has its page, opening with its title, and its tooltip in the files the mod ships.</summary>
    [Fact]
    public void EveryTermHasItsPageAndTooltip()
    {
        var sim = Simulation.Fresh();

        foreach (var term in sim.Editor.Codex.All)
        {
            Assert.True(term.Name != term.Id, term.Id + " has no page, or a page without its title");
            Assert.False(string.IsNullOrWhiteSpace(term.Page), term.Id + " has nothing on its page after its title");
            Assert.False(string.IsNullOrWhiteSpace(term.Summary), term.Id + " has no tooltip");
        }
    }

    /// <summary>The codex is read in the language the editor speaks: its titles, its pages and its tooltips.</summary>
    [Fact]
    public void TheCodexIsReadInTheEditorsLanguage()
    {
        var sim = Simulation.Fresh();
        sim.Texts.Book.Pages = new CodexFiles(CodexFiles.Beside, "no");

        var term = new Terms(sim.Texts).Find(Terms.LogicSystem)!;

        Assert.Equal("Logikksystem", term.Name);
        Assert.StartsWith("Et ![](mark:logic-system) **logikksystem**", term.Summary);
        Assert.Contains("[konstruksjonen](#construct)", term.Page);
    }

    /// <summary>Every attribute a codex page's boxes ask for, in every language, is markdown-lite's own or one the codex gives a look.</summary>
    [Fact]
    public void EveryBoxInTheCodexAsksForSomethingItHas()
    {
        var native = new[] { "left", "center", "right" };
        foreach (var page in Directory.GetFiles(CodexFiles.Beside, "*.md", SearchOption.AllDirectories))
        {
            foreach (var name in Names(Markdown.Parse(File.ReadAllText(page)).Blocks))
            {
                Assert.True(native.Contains(name, StringComparer.OrdinalIgnoreCase) || Styles.Boxes.Find(name) != null,
                    Path.GetFileName(page) + " asks for " + name + ", which nothing gives a look");
            }
        }
    }

    /// <summary>Every kind icon a codex page asks for, in every language, is a mark the editor can draw.</summary>
    [Fact]
    public void EveryIconInTheCodexIsAMark()
    {
        var marks = new MarkImages(null);
        foreach (var page in Directory.GetFiles(CodexFiles.Beside, "*.md", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(page);
            for (var at = text.IndexOf("](mark:", StringComparison.Ordinal); at >= 0;
                 at = text.IndexOf("](mark:", at + 1, StringComparison.Ordinal))
            {
                var end = text.IndexOf(')', at);
                var locator = text.Substring(at + 2, end - at - 2);
                Assert.True(marks.Find(locator) != null, Path.GetFileName(page) + " asks for " + locator + ", which is no mark");
            }
        }
    }

    /// <summary>A page a translation lacks is read in English.</summary>
    [Fact]
    public void APageATranslationLacksIsReadInEnglish()
    {
        var pages = new CodexFiles(CodexFiles.Beside, "xx");

        Assert.StartsWith("# Logic system", pages.Find("logic-system.md"));
    }

    /// <summary>Every link in every codex page names a term the codex has, so no page leads nowhere.</summary>
    [Fact]
    public void EveryLinkInTheCodexLeadsToATerm()
    {
        var sim = Simulation.Fresh();
        foreach (var term in sim.Editor.Codex.All)
        {
            var page = term.Page;
            for (var at = page.IndexOf("](#", StringComparison.Ordinal); at >= 0;
                 at = page.IndexOf("](#", at + 1, StringComparison.Ordinal))
            {
                var end = page.IndexOf(')', at);
                var target = page.Substring(at + 2, end - at - 2);
                Assert.True(sim.Editor.Codex.Named(target) != null, term.Id + " links to " + target + ", which the codex lacks");
            }
        }
    }

    /// <summary>The first point on the screen where a link sits inside <paramref name="inside" />, or anywhere where that is null, and the text it sits in.</summary>
    static Vector2 FirstLink(Screen screen, out RichText text, Lumel.Element? inside = null)
    {
        for (var y = 0; y < 1080; y += 2)
        {
            for (var x = 0; x < 2000; x += 2)
            {
                var point = new Vector2(x, y);
                if (screen.Scene.Under(point) is RichText rich && rich.LinkAt(point) != null && (inside == null || Within(rich, inside)))
                {
                    text = rich;
                    return point;
                }
            }
        }

        text = null!;
        Assert.Fail("no link on the screen");
        return default;
    }

    /// <summary>Whether <paramref name="element" /> is <paramref name="outer" /> or inside it.</summary>
    static bool Within(Lumel.Element element, Lumel.Element outer)
    {
        for (var at = element; at != null; at = at.Parent)
        {
            if (at == outer)
                return true;
        }

        return false;
    }

    /// <summary>A codex's files held in memory, by name.</summary>
    sealed class Pages : ITextSource
    {
        readonly Dictionary<string, string> _files = new Dictionary<string, string>();

        public Pages(params string[] pairs)
        {
            for (var i = 0; i + 1 < pairs.Length; i += 2)
                _files[pairs[i]] = pairs[i + 1];
        }

        public string? Find(string key) => _files.TryGetValue(key, out var text) ? text : null;
    }

    /// <summary>Every attribute the boxes in <paramref name="blocks" /> name, inside quotes and other boxes too.</summary>
    static IEnumerable<string> Names(IList<Block> blocks) =>
        blocks.SelectMany(block => block.Names.Concat(Names(block.Blocks)));
}
