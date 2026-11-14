using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Xunit;

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
    readonly AgoText _ago = new AgoText(new Texts());

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

    /// <summary>A length is said in the unit the program writes it in.</summary>
    [Fact]
    public void ALengthIsSaidInItsOwnUnit()
    {
        Assert.Equal("5 seconds", TimeText.Duration(Duration.OfSeconds(5)));
        Assert.Equal("1.5 seconds", TimeText.Duration(Duration.OfSeconds(1.5)));
        Assert.Equal("1 second", TimeText.Duration(Duration.OfSeconds(1)));
        Assert.Equal("3600 ticks", TimeText.Duration(Duration.OfTicks(3600)));
    }
}
