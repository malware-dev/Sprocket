using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Sprocket.Host.SpaceEngineers.Framework.Versioning;
using VRage.Game.ModAPI.Ingame.Utilities;
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
        var written = new Settings(new SettingsLayer(true, 10), new SettingsLayer(null, 100));
        var read = Settings.Of(written.Written());

        Assert.True(read.Global.Same(written.Global));
        Assert.True(read.World.Same(written.World));
        Assert.True(read.DeveloperMode);
        Assert.Equal(100, read.FastestUpdate);
        Assert.Equal(10, read.DefaultFastestUpdate);
        Assert.False(Settings.Of(new Settings(false).Written()).DeveloperMode);
    }

    /// <summary>A player's tips are on unless their own section of the global file turns them off, and writing it leaves every world's settings as they were.</summary>
    [Fact]
    public void APlayersTipsAreOnUnlessTheyTurnedThemOff()
    {
        Assert.True(PlayerSettings.From(null).Tips);
        Assert.True(PlayerSettings.From(Ini("[Sprocket]\ndeveloperMode=true\n")).Tips);
        Assert.False(PlayerSettings.From(Ini("[Player]\ntips=false\n")).Tips);

        var file = Ini("[Sprocket]\ndeveloperMode=true\n");
        new PlayerSettings(true).WithTips(false).Write(file);

        Assert.False(PlayerSettings.From(file).Tips);
        Assert.True(Settings.From(file.ToString(), null).DeveloperMode);
    }

    /// <summary>The callouts a player was offered and dismissed live in their own section of the global file beside their tips, leaving every world's settings as they were.</summary>
    [Fact]
    public void APlayerKeepsWhatWasOfferedAndDismissed()
    {
        var none = PlayerSettings.From(Ini("[Player]\ntips=false\n"));
        Assert.Null(none.Seen);
        Assert.Empty(none.Dismissed);

        var file = Ini("[Sprocket]\ndeveloperMode=true\n");
        new PlayerSettings(true).WithSeen(new ModRelease(1, 3)).WithDismissed("share").WithDismissed("import").WithDismissed("share").Write(file);

        Assert.Contains("[Player]", file.ToString());
        Assert.Contains("seen=1.3", file.ToString());
        Assert.Contains("dismissed=share,import", file.ToString());

        var read = PlayerSettings.From(Ini(file.ToString()));
        Assert.Equal(new ModRelease(1, 3), read.Seen);
        Assert.Equal(new[] { "share", "import" }, read.Dismissed);
        Assert.True(read.HasDismissed("import"));
        Assert.False(read.WithTips(false).HasDismissed("settings"));
        Assert.Equal(new ModRelease(1, 3), read.WithTips(false).Seen);
        Assert.True(Settings.From(file.ToString(), null).DeveloperMode);
    }

    /// <summary>
    /// A file of release 1 says its release as a whole number, which reads as that release of major version 1 and is
    /// written back as a version; a release newer than it is still offered.
    /// </summary>
    [Fact]
    public void AReleaseOneFilesWholeNumberReadsAsAVersion()
    {
        var read = PlayerSettings.From(Ini("[Player]\nseen=1\n"));
        Assert.Equal(new ModRelease(1, 1), read.Seen);
        Assert.True(new ModRelease(1, 2) > read.Seen!.Value);

        var file = Ini("");
        read.Write(file);
        Assert.Contains("seen=1.1", file.ToString());
    }

    static MyIni Ini(string text)
    {
        var ini = new MyIni();
        Assert.True(ini.TryParse(text));
        return ini;
    }

    /// <summary>The debug log is off unless a file says otherwise, the world's file winning, and it is handed to a client as it is.</summary>
    [Fact]
    public void TheDebugLogIsOffUnlessSaid()
    {
        Assert.False(Settings.From(null, null).DebugLog);
        Assert.True(Settings.From("[Sprocket]\ndebugLog=true\n", null).DebugLog);
        Assert.False(Settings.From("[Sprocket]\ndebugLog=true\n", "[Sprocket]\ndebugLog=false\n").DebugLog);

        var written = new Settings(SettingsLayer.Empty, new SettingsLayer(null, null, true));
        Assert.True(Settings.Of(written.Written()).DebugLog);
        Assert.True(Settings.Of(written.Written()).World.Same(written.World));
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
        var from = GameLog.Lines.Count;

        Assert.Equal(taken, Settings.From(null, "[Sprocket]\nfastestUpdate=" + written + "\n").FastestUpdate);
        Assert.Contains(GameLog.Lines.Skip(from), line => line.Contains("fastestUpdate as " + written) && line.Contains("taken as " + taken));
    }

}
