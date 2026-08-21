using Reactor.Ui;
using Xunit;

namespace Reactor.Host.Tests;

/// <summary>
/// This host's answer to everything core says a target must provide.
/// </summary>
/// <remarks>
/// Core states capabilities and leaves presentation alone, which leaves a hole: nothing makes
/// a host actually draw one of them. The hole cannot be closed by testing - no test knows
/// whether a drag target is big enough to hit, or whether a controller can reach it.
///
/// What this closes is forgetting. Adding a point to <see cref="Conformance"/> breaks this
/// test until somebody writes down what this host does about it. Self-reported and worth it
/// anyway: it turns "nobody thought about it" into "somebody decided", which is the same trade
/// the undo round-trip tests make.
///
/// Answering with "not yet" is allowed and is not cheating - it is the difference between a
/// known gap and an unknown one.
/// </remarks>
public class ConformanceTests
{
    static readonly Dictionary<string, string> Answers = new()
    {
        ["reorder-steps"] =
            "A grip at the left of each step, dragged to where the line should go, sending the "
            + "dropped position to MoveToCommand as one move. The rows between here and there "
            + "slide aside so the gap it would land in is visible. Nothing else offers it: "
            + "there is no keyboard route and no route without a pointer, which is a real gap "
            + "for a target that will one day be driven from a controller.",

        ["narrow-a-long-list"] =
            "A search box at the top of every stage that says it can be narrowed, present for "
            + "the whole life of the stage rather than appearing once a list gets long.",

        ["a-way-back"] =
            "A Back button at the left of the header, on the same row as the heading.",

        ["say-when-it-cannot-be-changed"] =
            "A line under the header while a sequence runs, saying to stop it to make changes. "
            + "The editing controls are absent rather than greyed, so the line is the only "
            + "thing telling somebody the add button is waiting rather than gone."
    };

    [Fact]
    public void ThisHostHasAnsweredEveryConformancePoint()
    {
        var unanswered = Conformance.Points
            .Where(point => !Answers.ContainsKey(point.Id))
            .Select(point => $"\n  {point.Id}: {point.Must}\n    Why: {point.Why}")
            .ToList();

        Assert.True(unanswered.Count == 0,
            "Core asks a target to provide something this host has not said anything about. "
            + "Add an entry to Answers saying what this host does, or \"not yet\" if it does "
            + "nothing:" + string.Join("", unanswered));
    }

    /// <summary>
    /// An answer to a point core no longer makes is dead weight and hides the live ones.
    /// </summary>
    [Fact]
    public void ThisHostDoesNotAnswerPointsCoreNoLongerMakes()
    {
        var stale = Answers.Keys
            .Where(id => !Conformance.Points.Any(point => point.Id == id))
            .ToList();

        Assert.True(stale.Count == 0,
            "This host answers points core no longer asks: " + string.Join(", ", stale));
    }
}
