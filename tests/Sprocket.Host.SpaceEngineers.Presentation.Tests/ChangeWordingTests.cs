using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>An arrow's words are put together again whenever anything they were put together from has moved.</summary>
public class ChangeWordingTests
{
    /// <summary>The same change, edits, bindings and round say the same words; any one of them moved, and they are put together again.</summary>
    [Fact]
    public void EveryPartOfWhatTheWordsCameFromIsLookedAt()
    {
        var change = new object();
        var screen = new object();
        var wording = new ChangeWording();

        Assert.False(wording.Says(change, screen, 1, 1, 1));
        Assert.True(wording.Says(change, screen, 1, 1, 1));

        Assert.False(wording.Says(new object(), screen, 1, 1, 1));
        Assert.False(wording.Says(change, screen, 1, 1, 1));
        Assert.False(wording.Says(change, new object(), 1, 1, 1));
        Assert.False(wording.Says(change, screen, 1, 1, 1));
        Assert.False(wording.Says(change, screen, 2, 1, 1));
        Assert.False(wording.Says(change, screen, 2, 2, 1));
        Assert.False(wording.Says(change, screen, 2, 2, 2));
        Assert.True(wording.Says(change, screen, 2, 2, 2));
    }
}
