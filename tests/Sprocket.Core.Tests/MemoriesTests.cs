using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>What the world remembers of the runtime is read whole as it loads, and what will not read is started afresh.</summary>
public class MemoriesTests
{
    /// <summary>A state that will not read says why, gives a fresh memory, and the next save writes over it.</summary>
    [Fact]
    public void AStateThatWillNotReadStartsAfresh()
    {
        var memories = new Memories(new ProtoBinary());

        memories.Read("this is no state", 100);

        Assert.Contains("would not read, and starts afresh", memories.Trouble);
        Assert.Empty(memories.Claim(42).Runs);
        Assert.Equal(100, memories.Now(100));

        var written = memories.Write(100, new List<ConstructMemory>());
        Assert.NotEqual("this is no state", written);
        Assert.Equal(100, new ProtoBinary().Unpack<WorldMemory>(written).Clock);
    }
}
