using Sprocket.Host.SpaceEngineers.Framework.Game.Copies;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Tests;

/// <summary>
/// Which mod the stable copy stands down for: Sprocket's own Bleeding Edge, and no other mod's.
/// </summary>
public class BleedingEdgeTests
{
    [Theory]
    [InlineData("Sprocket Automation (Bleeding Edge)", 3807234771UL)]
    [InlineData("Sprocket Automation (Bleeding Edge)", 0UL)]
    [InlineData("Renamed by the player", 3807234771UL)]
    public void SprocketsBleedingEdgeIsKnownByItsItemOrItsName(string name, ulong itemId)
    {
        Assert.True(BleedingEdge.Is(name, itemId));
    }

    [Theory]
    [InlineData("LINK (Bleeding Edge)", 3831130955UL)]
    [InlineData("Physicsless Elevator (Bleeding Edge)", 0UL)]
    [InlineData("Sprocket Automation", 3841033860UL)]
    [InlineData(null, 0UL)]
    public void AnotherModsBleedingEdgeIsNotSprockets(string? name, ulong itemId)
    {
        Assert.False(BleedingEdge.Is(name, itemId));
    }

    [Theory]
    [InlineData("Sprocket Automation (Bleeding Edge)", "Sprocket Automation (Bleeding Edge)", true)]
    [InlineData("Sprocket Automation (Bleeding Edge)", "3807234771.sbm", true)]
    [InlineData("Sprocket Automation", "3841033860.sbm", false)]
    [InlineData("Sprocket Automation", "Sprocket Automation", false)]
    [InlineData(null, null, false)]
    public void TheRunningCopyKnowsWhetherItIsBleedingEdge(string? modName, string? modId, bool expected)
    {
        Assert.Equal(expected, BleedingEdge.IsRunning(modName, modId));
    }
}
