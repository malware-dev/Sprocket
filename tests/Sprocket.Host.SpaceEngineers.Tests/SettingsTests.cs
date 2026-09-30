using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Tests;

/// <summary>
/// Two files, merged: the world's answer where it has one, the global one otherwise.
/// </summary>
/// <remarks>
/// The whole point of the pair is that a world saying nothing leaves the global answer standing,
/// which is a different thing from a world saying no. Both are here.
/// </remarks>
public class SettingsTests
{
    const string On = "[Sprocket]\ndeveloperMode=true\n";
    const string Off = "[Sprocket]\ndeveloperMode=false\n";

    [Fact]
    public void NothingAnywhereIsOff()
    {
        Assert.False(Settings.From(null, null).DeveloperMode);
    }

    [Fact]
    public void GlobalAloneDecides()
    {
        Assert.True(Settings.From(On, null).DeveloperMode);
    }

    [Fact]
    public void WorldAloneDecides()
    {
        Assert.True(Settings.From(null, On).DeveloperMode);
    }

    [Fact]
    public void WorldBeatsGlobal()
    {
        Assert.False(Settings.From(On, Off).DeveloperMode);
        Assert.True(Settings.From(Off, On).DeveloperMode);
    }

    /// <summary>A world with a file but nothing to say about this leaves the global answer alone.</summary>
    [Fact]
    public void SilenceIsNotDisagreement()
    {
        Assert.True(Settings.From(On, "[Sprocket]\n").DeveloperMode);
        Assert.True(Settings.From(On, "[Something]\nelse=true\n").DeveloperMode);
    }

    /// <summary>A file somebody mistyped is ignored, not fatal, and leaves the other one standing.</summary>
    [Fact]
    public void RubbishIsIgnored()
    {
        Assert.True(Settings.From(On, "this is not an ini file at all").DeveloperMode);
    }

    /// <summary>How often a program may look holds nothing back where nothing says otherwise, and the world's count beats the global one.</summary>
    [Fact]
    public void TheQuickestLookIsEveryTickUnlessSaid()
    {
        Assert.Equal(1, Settings.From(null, null).QuickestLook);
        Assert.Equal(10, Settings.From("[Sprocket]\nquickestLook=10\n", null).QuickestLook);
        Assert.Equal(30, Settings.From("[Sprocket]\nquickestLook=10\n", "[Sprocket]\nquickestLook=30\n").QuickestLook);
        Assert.Equal(1, Settings.From("[Sprocket]\nquickestLook=-4\n", null).QuickestLook);
    }
}
