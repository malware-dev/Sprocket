using System.IO;
using System.Xml.Linq;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Sprocket.Sim.Saves;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The harness reads what a saved world remembers of the runtime, which the save keeps in binary, as text.</summary>
public class SavedStateTests : IDisposable
{
    static readonly Guid Airlock = new Guid("6f1e2d3c-4b5a-4978-8a1b-2c3d4e5f6a7b");

    readonly string _folder = Path.Combine(Path.GetTempPath(), "sprocket-saved-state-" + Guid.NewGuid().ToString("N"));

    public SavedStateTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    /// <summary>Every remembered member is written by its name, nested members and lists included.</summary>
    [Fact]
    public void EveryRememberedMemberIsWrittenByName()
    {
        var world = new WorldMemory { Clock = 1234 };
        var construct = new ConstructMemory { Keeper = 42 };
        construct.Runs.Add(new RunMemory { System = Airlock, Sequence = 1, Step = 3 });
        world.Constructs.Add(construct);
        world.Players.Add(new PlayerMemory { Player = 7, Ticks = true });
        Sector(world);

        var said = SavedState.Render(_folder);

        Assert.Contains("Clock: 1234\n", said);
        Assert.Contains("Keeper: 42\n", said);
        Assert.Contains("System: " + Airlock + "\n", said);
        Assert.Contains("Step: 3\n", said);
        Assert.Contains("Ticks: True\n", said);
    }

    /// <summary>A world Sprocket has saved nothing in says so.</summary>
    [Fact]
    public void AWorldWithNoCarrierSaysSo()
    {
        new XDocument(new XElement("MyObjectBuilder_Sector")).Save(Path.Combine(_folder, "SANDBOX_0_0_0_.sbs"));

        Assert.Contains("The world has no carrier", SavedState.Render(_folder));
    }

    /// <summary>Writes a sector whose carrier keeps <paramref name="world" /> as its state, laid out as the game saves mod storage.</summary>
    void Sector(WorldMemory world)
    {
        var files = new CarrierFiles();
        files.Write(Memories.File, new ProtoBinary().Pack(world));

        new XDocument(new XElement("MyObjectBuilder_Sector",
            new XElement("Storage",
                new XElement("dictionary",
                    new XElement("item",
                        new XElement("Key", Carrier.Key.ToString("D")),
                        new XElement("Value", files.Pack()))))))
            .Save(Path.Combine(_folder, "SANDBOX_0_0_0_.sbs"));
    }
}
