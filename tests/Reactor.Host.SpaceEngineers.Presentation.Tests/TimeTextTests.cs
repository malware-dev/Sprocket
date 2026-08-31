using Reactor.Host.SpaceEngineers.Presentation.Words;
using Xunit;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The words this target puts on a reading, and what saying them costs.
/// </summary>
/// <remarks>
/// Core states how long ago something happened in ticks, because ticks are the only honest unit it
/// has. Turning that into something a person reads is this target's job, and it is done for every
/// visible sequence on every tick - so the cost of doing it is part of the behaviour rather than an
/// implementation detail.
/// </remarks>
public class TimeTextTests
{
    readonly TimeText _time = new TimeText();

    [Fact]
    public void HowLongAgoReadsInTheUnitsAPersonWouldUse()
    {
        Assert.Equal("", _time.Ago(-1));
        Assert.Equal("just now", _time.Ago(0));
        Assert.Equal("just now", _time.Ago(119));
        Assert.Equal("3 seconds ago", _time.Ago(180));
        Assert.Equal("a minute ago", _time.Ago(60 * 60));
        Assert.Equal("5 minutes ago", _time.Ago(60 * 60 * 5));
        Assert.Equal("over an hour ago", _time.Ago(60 * 60 * 60));
    }

    /// <summary>
    /// Saying it again costs nothing, because it is the same string rather than an equal one.
    /// </summary>
    /// <remarks>
    /// Every answer comes from a table built once. Without that, a screen showing ten sequences
    /// would build ten strings sixty times a second, inside the game's update loop, to say what it
    /// already said last frame.
    /// </remarks>
    [Fact]
    public void SayingItAgainAllocatesNothing()
    {
        Assert.Same(_time.Ago(180), _time.Ago(180));
        Assert.Same(_time.Ago(181), _time.Ago(200));
        Assert.Same(_time.Ago(60 * 60 * 5), _time.Ago(60 * 60 * 5 + 59));
        Assert.Same(_time.Ago(0), _time.Ago(60));
    }

    /// <summary>Choosing a wait is where the two vocabularies have to meet.</summary>
    [Fact]
    public void AWaitIsOfferedInBothUnitsAtOnce()
    {
        Assert.Equal("5 seconds - 300 ticks", _time.WaitChoice(300));
        Assert.Equal("a minute - 3600 ticks", _time.WaitChoice(3600));
    }
}
