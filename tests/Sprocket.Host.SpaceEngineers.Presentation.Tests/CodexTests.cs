using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.Home;
using Sprocket.Sim.Machine;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

public class CodexTests
{
    static Simulation Home()
    {
        var sim = Simulation.Fresh();
        sim.Tick();
        return sim;
    }

    /// <summary>The question mark on the home screen opens the codex at the home screen's entry, which Back cannot leave but the cross closes.</summary>
    [Fact]
    public void TheQuestionMarkOpensTheScreensEntry()
    {
        var sim = Home();

        sim.Editor.Screen.Help!.Execute();
        sim.Tick();

        var page = Assert.IsType<CodexPage>(sim.Editor.Current);
        Assert.Equal(Terms.Home, page.Term.Id);
        Assert.True(sim.Editor.Screen.Browses);
        Assert.Null(sim.Editor.Screen.StepsBack);
        Assert.Null(sim.Editor.Screen.StepsForward);

        sim.Editor.Back();
        sim.Tick();
        Assert.IsType<HomePage>(sim.Editor.Current);
    }

    /// <summary>The codex keeps a history of its own: moving on drops what was stepped back from, and closing it from any depth leads home in one press.</summary>
    [Fact]
    public void TheCodexStepsThroughAHistoryOfItsOwn()
    {
        var sim = Home();
        var codex = sim.Editor.Codex;
        sim.Editor.OpenCodex(codex.Find(Terms.LogicSystem)!);
        sim.Tick();
        var page = Assert.IsType<CodexPage>(sim.Editor.Current);

        page.Go(codex.Find(Terms.Installation));
        page.Go(codex.Find(Terms.Sequence));
        sim.Tick();
        Assert.Equal(Terms.Sequence, page.Term.Id);

        sim.Editor.Screen.StepsBack!.Execute();
        sim.Editor.Screen.StepsBack!.Execute();
        sim.Tick();
        Assert.Equal(Terms.LogicSystem, page.Term.Id);
        Assert.Null(sim.Editor.Screen.StepsBack);

        sim.Editor.Screen.StepsForward!.Execute();
        sim.Tick();
        Assert.Equal(Terms.Installation, page.Term.Id);

        page.Go(codex.Find(Terms.Library));
        sim.Tick();
        Assert.Null(sim.Editor.Screen.StepsForward);

        sim.Editor.Back();
        sim.Tick();
        Assert.IsType<HomePage>(sim.Editor.Current);
    }

    /// <summary>The codex lists every term in its margin, and pressing one moves there as a step forward, with no question mark of its own.</summary>
    [Fact]
    public void TheMarginListsEveryTermAndMovesThere()
    {
        var sim = Home();
        sim.Editor.OpenCodex(sim.Editor.Codex.Find(Terms.Installation)!);
        sim.Tick();
        var page = Assert.IsType<CodexPage>(sim.Editor.Current);
        Assert.Null(sim.Editor.Screen.Help);

        page.Go(sim.Editor.Codex.Find(Terms.Library));
        sim.Tick();
        Assert.Equal(Terms.Library, page.Term.Id);

        sim.Editor.Screen.StepsBack!.Execute();
        sim.Tick();
        Assert.Equal(Terms.Installation, page.Term.Id);
    }

    /// <summary>The first point on the screen where a link sits, and the text it sits in.</summary>
    static Vector2 FirstLink(Screen screen, out RichText text)
    {
        for (var y = 0; y < 400; y += 2)
        {
            for (var x = 0; x < 2000; x += 2)
            {
                var point = new Vector2(x, y);
                if (screen.Scene.Under(point) is RichText rich && rich.LinkAt(point) != null)
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

    /// <summary>The home screen's heading is a link to the logic system's entry: resting on it shows the term's preview, and pressing it opens the codex there.</summary>
    [Fact]
    public void TheHeadingLeadsIntoTheCodex()
    {
        var screen = Screen.Fresh("tick", "3");
        var at = FirstLink(screen, out var heading);
        var term = screen.Scene.Editor.Codex.Find(Terms.LogicSystem)!;

        Assert.Same(term.Preview, heading.TipAt(at));

        screen.Do(((int)at.X).ToString(), ((int)at.Y).ToString(), "tick", "3");

        var page = Assert.IsType<CodexPage>(screen.Scene.Editor.Current);
        Assert.Equal(Terms.LogicSystem, page.Term.Id);
    }

    /// <summary>A link inside a codex page moves the codex on to its term, as a step forward in the codex's own history.</summary>
    [Fact]
    public void ALinkInAPageMovesTheCodexOn()
    {
        var screen = Screen.Fresh("tick", "3");
        var editor = screen.Scene.Editor;
        editor.OpenCodex(editor.Codex.Find(Terms.LogicSystem)!);
        screen.Do("tick", "3");

        // The margin is buttons, so the first link on the screen is the page's own.
        var at = FirstLink(screen, out _);
        screen.Do(((int)at.X).ToString(), ((int)at.Y).ToString(), "tick", "3");

        var page = Assert.IsType<CodexPage>(editor.Current);
        Assert.Equal(Terms.Installation, page.Term.Id);
        Assert.NotNull(editor.Screen.StepsBack);
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
}
