using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A step is added through one plus that hangs a menu of every kind of step, at the end of a sequence and of each branch.</summary>
public class StepMenuTests
{
    const string Kinds = "press \"logic system\" type Airlock enter press Airlock press sequence enter";

    /// <summary>The plus at the end of a sequence hangs the five kinds of step from itself, and nothing else is offered there.</summary>
    [Fact]
    public void ThePlusHangsEveryKindOfStep()
    {
        var screen = Screen.Fresh(Kinds.Split(' '));

        Assert.True(screen.Shows("+ step"));
        Assert.False(screen.Shows("task"), "the kinds were offered before the plus was pressed");

        screen.Do("press", "step");

        foreach (var kind in new[] { "task", "wait", "run", "stop", "if" })
            Assert.True(screen.Shows(kind), "the menu does not offer " + kind);
    }

    /// <summary>Choosing a kind puts the menu away and adds that step, opening what it opens as its own button did.</summary>
    [Fact]
    public void ChoosingAKindAddsIt()
    {
        var screen = Screen.Fresh((Kinds + " press step press wait").Split(' '));
        screen.Run(1);

        Assert.False(screen.Shows("task"), "the menu was left open");
        Assert.IsType<Wait>(screen.Editor.Program.Systems[0].Sequences[0].Steps[0]);
        Assert.NotNull(Assert.IsType<SequencePage>(screen.Editor.Current).Lengths);
    }

    /// <summary>An if's closing row offers both branches under one plus, and once it has an otherwise, the one left as itself.</summary>
    [Fact]
    public void APlusWithOneChoiceLeftShowsThatChoice()
    {
        var screen = Screen.Fresh((Kinds + " press step press if press \"Airlock Outer Door\" press closed press Done").Split(' '));

        Assert.True(screen.Shows("+ branch"));
        Assert.False(screen.Shows("+ otherwise if"), "the branches were offered apart before one was taken");

        screen.Do("press", "branch", "press", "otherwise");

        Assert.True(screen.Shows("+ otherwise if"));
        Assert.False(screen.Shows("+ branch"), "a plus with one choice left still offered a menu");
    }

    /// <summary>The logic system's plus for triggers offers both kinds of trigger, and choosing one starts making it.</summary>
    [Fact]
    public void ThePlusForTriggersOffersBothKinds()
    {
        var screen = Screen.Fresh("press \"logic system\" type Airlock enter press Airlock press trigger".Split(' '));

        Assert.True(screen.Shows("when"));
        Assert.True(screen.Shows("every"));

        screen.Do("press", "when");

        Assert.IsType<PickPage>(screen.Editor.Current);
    }

    /// <summary>The end of an if's branch has its own plus, and a step chosen there goes into the branch.</summary>
    [Fact]
    public void TheEndOfABranchHasItsOwnPlus()
    {
        var screen = Screen.Fresh((Kinds + " press step press if press \"Airlock Outer Door\" press closed press Done").Split(' '));

        var rows = screen.Editor.StepRows();
        Assert.Single(rows, row => row.Kind == StepKind.Adds);

        screen.Do("press", "step", "press", "wait");
        screen.Run(1);

        var steps = screen.Editor.Program.Systems[0].Sequences[0].Steps;
        Assert.IsType<If>(steps[0]);
        Assert.IsType<Wait>(steps[1]);
        Assert.IsType<EndIf>(steps[2]);
    }
}
