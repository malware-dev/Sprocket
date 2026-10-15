using System.IO;
using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A change an admin makes is kept in the file of its level alone, the global file for every world and the world's own for this
/// world, and leaves whatever else that file says as it was.
/// </summary>
public class SettingsFilesTests
{
    const string Global = "global";
    const string World = "world";

    readonly Dictionary<string, string> _files = new Dictionary<string, string>();
    readonly List<string> _written = new List<string>();

    /// <summary>A change to this world writes the world's file, and the global file is left untouched.</summary>
    [Fact]
    public void AChangeToThisWorldWritesTheWorldsFileAlone()
    {
        _files[Global] = "[Sprocket]\nfastestUpdate=10\n";
        _files[World] = "[Sprocket]\ndeveloperMode=true\n";

        Storing(() =>
        {
            var was = Settings.Read();
            was.WithWorld(was.World.WithFastestUpdate(100)).Keep(was);
        });

        Assert.Equal(new[] { World }, _written);
        var read = Settings.From(_files[Global], _files[World]);
        Assert.Equal(100, read.FastestUpdate);
        Assert.True(read.DeveloperMode);
        Assert.Equal(10, read.DefaultFastestUpdate);
    }

    /// <summary>A change to every world writes the global file, and the world's file is left untouched.</summary>
    [Fact]
    public void AChangeToEveryWorldWritesTheGlobalFileAlone()
    {
        _files[Global] = "[Sprocket]\nfastestUpdate=10\n";

        Storing(() =>
        {
            var was = Settings.Read();
            was.WithGlobal(was.Global.WithDeveloperMode(true)).Keep(was);
        });

        Assert.Equal(new[] { Global }, _written);
        Assert.False(_files.ContainsKey(World));
        var read = Settings.From(_files[Global], null);
        Assert.True(read.DeveloperMode);
        Assert.Equal(10, read.FastestUpdate);
    }

    /// <summary>The default under this world takes the setting out of the world's file, and the rest of the file stays as written.</summary>
    [Fact]
    public void TheDefaultTakesTheSettingOutOfTheFile()
    {
        _files[World] = "[Sprocket]\nfastestUpdate=100\ndeveloperMode=true\n\n[Other]\nkept=yes\n";

        Storing(() =>
        {
            var was = Settings.Read();
            was.WithWorld(was.World.WithFastestUpdate(null)).Keep(was);
        });

        Assert.DoesNotContain("fastestUpdate", _files[World]);
        Assert.Contains("developerMode=true", _files[World]);
        Assert.Contains("kept=yes", _files[World]);
    }

    /// <summary>Runs <paramref name="act" /> with the game's mod storage holding <see cref="_files" />, writes noted in <see cref="_written" />.</summary>
    void Storing(Action act)
    {
        var was = MyAPIGateway.Utilities;
        MyAPIGateway.Utilities = Pretend.To<IMyUtilities>()
            .Does("FileExistsInGlobalStorage", _ => _files.ContainsKey(Global))
            .Does("FileExistsInWorldStorage", _ => _files.ContainsKey(World))
            .Does("ReadFileInGlobalStorage", _ => new StringReader(_files[Global]))
            .Does("ReadFileInWorldStorage", _ => new StringReader(_files[World]))
            .Does("WriteFileInGlobalStorage", _ => new Kept(this, Global))
            .Does("WriteFileInWorldStorage", _ => new Kept(this, World))
            .Done();
        try
        {
            act();
        }
        finally
        {
            MyAPIGateway.Utilities = was;
        }
    }

    /// <summary>A file being written, which takes the place of the old one as it closes.</summary>
    sealed class Kept : StringWriter
    {
        readonly SettingsFilesTests _owner;
        readonly string _name;

        public Kept(SettingsFilesTests owner, string name)
        {
            _owner = owner;
            _name = name;
        }

        protected override void Dispose(bool disposing)
        {
            _owner._files[_name] = ToString();
            _owner._written.Add(_name);
            base.Dispose(disposing);
        }
    }
}
