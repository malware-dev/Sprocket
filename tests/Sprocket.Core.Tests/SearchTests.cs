using Sprocket.Host.SpaceEngineers.Features.Selector.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Xunit;

namespace Sprocket.Tests;

/// <summary>What typing into a search box finds: each word anywhere, whatever its case, in the order typed.</summary>
public class SearchTests
{
    /// <summary>A search finds a name where each word typed is found, whatever its case, in the order typed, each starting only where the one before it ended.</summary>
    [Theory]
    [InlineData("Airlock 2", "airloc 2", true)]
    [InlineData("Airlock 2", "AIRLOCK", true)]
    [InlineData("Airlock 1", "airl 1", true)]
    [InlineData("Airlock 10", "airl 1", true)]
    [InlineData("Airlock Door 1", "airl 1", true)]
    [InlineData("Airlock Inner Door", "  air   inn  ", true)]
    [InlineData("Airlock Inner Door", "", true)]
    [InlineData("Airlock 2", "airloc 3", false)]
    [InlineData("Airlock Inner Door", "door outer", false)]
    [InlineData("Airlock", "airlocks", false)]
    [InlineData("1 Airlock", "airl 1", false)]
    [InlineData("Airlock Inner Door", "door inner", false)]
    [InlineData("Airlock Inner Door", "inn air", false)]
    [InlineData("Airlock", "airlock lock", false)]
    [InlineData("Airlock Lock", "airlock lock", true)]
    public void EveryWordTypedIsFoundInTheOrderTyped(string name, string search, bool found)
    {
        Assert.Equal(found, EntityQuery.MatchesText(name, search));
    }

    [Fact]
    public void ABlockIsFoundByItsName()
    {
        Assert.True(EntityQuery.Matches(Block("Airlock Inner", "Sliding Door"), "airl inn", null, Family.None));
    }

    [Theory]
    [InlineData("sliding")]
    [InlineData("airlock sliding")]
    public void ItsKindIsNotSearched(string search)
    {
        Assert.False(EntityQuery.Matches(Block("Airlock Inner", "Sliding Door"), search, null, Family.None));
    }

    /// <summary>A block called <paramref name="name" />, of the kind shown as <paramref name="kind" />.</summary>
    static GridEntity Block(string name, string kind)
    {
        var ship = new Rigged();
        return new GridEntity(ship.Light(1, name).Block, new EntityKind("kind", kind));
    }
}
