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

    /// <summary>Settings written as one text read back the same at both levels, which is how the server hands them to a client.</summary>
    [Fact]
    public void WrittenSettingsReadBackTheSame()
    {
        var written = new Settings(new Layer(true, 10, null), new Layer(null, 100, Waiting.Step));
        var read = Settings.Of(written.Written());

        Assert.True(read.Global.Same(written.Global));
        Assert.True(read.World.Same(written.World));
        Assert.True(read.DeveloperMode);
        Assert.Equal(100, read.FastestUpdate);
        Assert.Equal(10, read.DefaultFastestUpdate);
        Assert.Equal(Waiting.Step, read.Waiting);
        Assert.False(Settings.Of(new Settings(false).Written()).DeveloperMode);
    }

    /// <summary>Each level keeps what it says apart, so a world leaving a setting can be told from one giving the same value.</summary>
    [Fact]
    public void EachLevelKeepsWhatItSays()
    {
        var read = Settings.From("[Sprocket]\nfastestUpdate=10\n", "[Sprocket]\ndeveloperMode=false\n");

        Assert.Equal(10, read.Global.FastestUpdate);
        Assert.Null(read.World.FastestUpdate);
        Assert.False(read.World.DeveloperMode);
        Assert.Null(read.Global.DeveloperMode);
    }

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

    /// <summary>The fastest update holds nothing back where nothing says otherwise, and the world's beats the global one.</summary>
    [Fact]
    public void TheFastestUpdateIsEveryTickUnlessSaid()
    {
        Assert.Equal(1, Settings.From(null, null).FastestUpdate);
        Assert.Equal(10, Settings.From("[Sprocket]\nfastestUpdate=10\n", null).FastestUpdate);
        Assert.Equal(100, Settings.From("[Sprocket]\nfastestUpdate=10\n", "[Sprocket]\nfastestUpdate=100\n").FastestUpdate);
    }

    /// <summary>A fastest update other than 1, 10 or 100 is said in the log and taken as the nearest of them.</summary>
    [Theory]
    [InlineData(-4, 1)]
    [InlineData(5, 1)]
    [InlineData(30, 10)]
    [InlineData(60, 100)]
    [InlineData(5000, 100)]
    public void AnotherFastestUpdateIsTakenAsTheNearest(int written, int taken)
    {
        var from = Told.Lines.Count;

        Assert.Equal(taken, Settings.From(null, "[Sprocket]\nfastestUpdate=" + written + "\n").FastestUpdate);
        Assert.Contains(Told.Lines.Skip(from), line => line.Contains("fastestUpdate as " + written) && line.Contains("taken as " + taken));
    }

    /// <summary>The old name for the fastest update is not read, so an old file leaves it at every tick.</summary>
    [Fact]
    public void TheOldNameIsNotRead()
    {
        Assert.Equal(1, Settings.From(null, "[Sprocket]\nquickestLook=100\n").FastestUpdate);
    }

    /// <summary>Waits are shown as a switch on each step unless the file says step, and a word it does not know is said in the log.</summary>
    [Fact]
    public void WaitsAreSwitchesUnlessSaid()
    {
        Assert.Equal(Waiting.Switch, Settings.From(null, null).Waiting);
        Assert.Equal(Waiting.Step, Settings.From(null, "[Sprocket]\nwaits=step\n").Waiting);
        Assert.Equal(Waiting.Switch, Settings.From("[Sprocket]\nwaits=step\n", "[Sprocket]\nwaits=switch\n").Waiting);

        var from = Told.Lines.Count;
        Assert.Equal(Waiting.Switch, Settings.From(null, "[Sprocket]\nwaits=bracket\n").Waiting);
        Assert.Contains(Told.Lines.Skip(from), line => line.Contains("waits as bracket"));
    }
}
