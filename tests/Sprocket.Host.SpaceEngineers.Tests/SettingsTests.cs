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
}
