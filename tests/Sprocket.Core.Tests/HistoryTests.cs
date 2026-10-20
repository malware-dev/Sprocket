using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Tests;

/// <summary>One list of changes in the order they were made, walked from each screen they touched.</summary>
public class HistoryTests
{
    /// <summary>Undo on a screen takes back the last change on it, and leaves a later change on another screen standing.</summary>
    [Fact]
    public void EachScreenTakesBackItsOwn()
    {
        var made = new List<string>();
        var history = new History<List<string>>();
        history.Did(made, new Mark("one", "A"), 0);
        history.Did(made, new Mark("two", "B"), 0);

        Assert.NotNull(history.Undo(made, "A"));

        Assert.Equal(new[] { "two" }, made);
        Assert.False(history.CanUndo(made, "A"));
        Assert.True(history.CanUndo(made, "B"));
    }

    /// <summary>
    /// A change that touched two screens goes back with every later change on either, latest first, says which screens it
    /// takes along, and comes back with them in the order they were made.
    /// </summary>
    [Fact]
    public void ASharedChangeTakesLaterOnesAlong()
    {
        var made = new List<string>();
        var history = new History<List<string>>();
        history.Did(made, new Mark("pick", "A", "B"), 0);
        history.Did(made, new Mark("describe", "B"), 0);
        history.Did(made, new Mark("elsewhere", "C"), 0);

        var also = new List<object>();
        history.Also(made, "A", true, also);
        Assert.Equal(new object[] { "B" }, also);

        history.Undo(made, "A");
        Assert.Equal(new[] { "elsewhere" }, made);

        Assert.True(history.CanRedo(made, "B"));
        history.Redo(made, "B");
        Assert.Equal(new[] { "elsewhere", "pick", "describe" }, made);
    }

    /// <summary>A new change on a screen ends what was taken back there, and leaves what was taken back elsewhere to make again.</summary>
    [Fact]
    public void ANewChangeEndsTheWayForwardOnItsScreen()
    {
        var made = new List<string>();
        var history = new History<List<string>>();
        history.Did(made, new Mark("one", "A"), 0);
        history.Did(made, new Mark("two", "B"), 0);
        history.Undo(made, "A");
        history.Undo(made, "B");

        history.Did(made, new Mark("three", "A"), 0);

        Assert.False(history.CanRedo(made, "A"));
        Assert.True(history.CanRedo(made, "B"));
    }

    /// <summary>After a walk, every change still standing says again the names it gave.</summary>
    [Fact]
    public void WhatStandsIsSaidAgainAfterAWalk()
    {
        var made = new List<string>();
        var history = new History<List<string>>();
        var standing = new Mark("rename", "S");
        history.Did(made, new Mark("step", "A"), 0);
        history.Did(made, standing, 0);

        history.Undo(made, "A");

        Assert.Equal(1, standing.Reasserted);
    }

    /// <summary>A change that adds its word to the list, touching the screens it is given.</summary>
    sealed class Mark : Edit<List<string>>
    {
        readonly string _word;
        readonly string[] _screens;

        public Mark(string word, params string[] screens)
        {
            _word = word;
            _screens = screens;
        }

        public int Reasserted { get; private set; }

        public override ChangeKind Change => ChangeKind.AddStep;

        public override void Apply(List<string> subject) => subject.Add(_word);

        public override void Undo(List<string> subject) => subject.Remove(_word);

        public override void Touches(List<string> subject, IList<object> screens)
        {
            foreach (var screen in _screens)
                screens.Add(screen);
        }

        public override void Reassert(List<string> subject) => Reasserted++;
    }
}
