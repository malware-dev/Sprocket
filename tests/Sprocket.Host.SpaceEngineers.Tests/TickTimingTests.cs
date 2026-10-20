using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Tests;

/// <summary>Sprocket's own time each tick, said once for every ten seconds of ticks.</summary>
public class TickTimingTests
{
    /// <summary>Nothing is said until a span of ticks is timed, then one line with the average, the worst and the constructs, and the next span starts afresh.</summary>
    [Fact]
    public void ALineIsSaidForEverySpanOfTicks()
    {
        var lines = new List<string>();
        var timing = new TickTiming(lines.Add);

        for (var i = 0; i < TickTiming.Span - 1; i++)
        {
            timing.Begin();
            timing.End(3);
        }

        Assert.Empty(lines);

        timing.Begin();
        timing.End(3);
        var line = Assert.Single(lines);
        Assert.Matches(@"^last 600 ticks: \d+\.\d µs a tick on average, worst \d+\.\d µs, 3 constructs with automation$", line);

        for (var i = 0; i < TickTiming.Span; i++)
        {
            timing.Begin();
            timing.End(4);
        }

        Assert.Equal(2, lines.Count);
        Assert.EndsWith("4 constructs with automation", lines[1]);
    }
}
