using Reactor.Host.SpaceEngineers.Screen;
using Xunit;

namespace Reactor.Host.SpaceEngineers.Tests;

/// <summary>
/// This target's answer to everything core says a target must provide.
/// </summary>
/// <remarks>
/// Core states capabilities and leaves presentation alone, which leaves a hole: nothing makes a target
/// actually draw one of them. The hole cannot be closed by testing - no test knows whether a drag target
/// is big enough to hit with a mouse the game is fighting you for.
///
/// What this closes is forgetting. Adding a point to <see cref="Conformance"/> breaks this test until
/// somebody writes down what this target does about it. Self-reported, and worth it anyway: it turns
/// "nobody thought about it" into "somebody decided".
///
/// Answering with "not yet" is allowed and is not cheating - it is the difference between a known gap and
/// an unknown one.
/// </remarks>
public class ConformanceTests
{
    static readonly Dictionary<string, string> Answers = new()
    {
        ["reorder-steps"] =
            "A grip at the left of each step, dragged to where the line should go, sending the "
            + "dropped position to MoveToCommand as one move. The rows between here and there slide "
            + "aside so the gap it would land in is visible. Two gaps, both real: there is no "
            + "keyboard route and no route without a pointer, and on this target a pointer costs the "
            + "player their mouse-look for as long as the editor is open.",

        ["narrow-a-long-list"] =
            "A search box at the top of every stage that says it can be narrowed, present for the "
            + "whole life of the stage rather than appearing once a list gets long, sending its text "
            + "to core on every keystroke. Beside it, one line naming the current kind, which opens "
            + "over the screen onto every kind actually present with its count - so somebody who does "
            + "not know the game calls it an air vent can read the word instead of typing it.",

        ["a-way-back"] =
            "A Back button at the left of the header, on the same row as the heading, holding its "
            + "place whether or not there is anywhere to go back to.",

        ["say-when-it-cannot-be-changed"] =
            "A line under the sequence header while it runs, saying to stop it to make changes. The "
            + "editing controls are absent rather than greyed and the slots read as plain words, so "
            + "that line is the only thing telling somebody the add button is waiting rather than gone."
    };

    [Fact]
    public void ThisTargetHasAnsweredEveryConformancePoint()
    {
        var unanswered = Conformance.Points
            .Where(point => !Answers.ContainsKey(point.Id))
            .Select(point => $"\n  {point.Id}: {point.Must}\n    Why: {point.Why}")
            .ToList();

        Assert.True(unanswered.Count == 0,
            "Core asks a target to provide something this one has not said anything about. Add an "
            + "entry to Answers saying what it does, or \"not yet\" if it does nothing:"
            + string.Join("", unanswered));
    }

    /// <summary>
    /// An answer to a point core no longer makes is dead weight and hides the live ones.
    /// </summary>
    [Fact]
    public void ThisTargetDoesNotAnswerPointsCoreNoLongerMakes()
    {
        var stale = Answers.Keys
            .Where(id => !Conformance.Points.Any(point => point.Id == id))
            .ToList();

        Assert.True(stale.Count == 0,
            "This target answers points core no longer asks: " + string.Join(", ", stale));
    }
}
