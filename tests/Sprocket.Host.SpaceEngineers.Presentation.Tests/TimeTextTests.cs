using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

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
    readonly Ago _ago = new Ago(new Texts());

    [Fact]
    public void HowLongAgoReadsInTheUnitsAPersonWouldUse()
    {
        Assert.Equal("", _ago.Said(-1));
        Assert.Equal("just now", _ago.Said(0));
        Assert.Equal("just now", _ago.Said(119));
        Assert.Equal("less than a minute ago", _ago.Said(180));
        Assert.Equal("less than a minute ago", _ago.Said(59 * 60));
        Assert.Equal("a minute ago", _ago.Said(60 * 60));
        Assert.Equal("5 minutes ago", _ago.Said(60 * 60 * 5));
        Assert.Equal("over an hour ago", _ago.Said(60 * 60 * 60));
    }

    /// <summary>
    /// Saying it again costs nothing, because it is the same string rather than an equal one.
    /// </summary>
    /// <remarks>
    /// Every answer comes from a table built once per language. Without that, a screen showing ten sequences
    /// would build ten strings sixty times a second, inside the game's update loop, to say what it
    /// already said last frame.
    /// </remarks>
    [Fact]
    public void SayingItAgainAllocatesNothing()
    {
        Assert.Same(_ago.Said(180), _ago.Said(180));
        Assert.Same(_ago.Said(181), _ago.Said(200));
        Assert.Same(_ago.Said(60 * 60 * 5), _ago.Said(60 * 60 * 5 + 59));
        Assert.Same(_ago.Said(0), _ago.Said(60));
    }

    /// <summary>Choosing a wait is where the two vocabularies have to meet.</summary>
    [Fact]
    public void AWaitIsOfferedInBothUnitsAtOnce()
    {
        Assert.Equal("5 seconds - 300 ticks", TimeText.WaitChoice(new Texts(), 300));
        Assert.Equal("a minute - 3600 ticks", TimeText.WaitChoice(new Texts(), 3600));
    }
}
