using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>What typing into a search box finds: each word anywhere, whatever its case, in the order typed.</summary>
public class SearchTests
{
    [Theory]
    [InlineData("Airlock 2", "airloc 2")]
    [InlineData("Airlock 2", "AIRLOCK")]
    [InlineData("Airlock 1", "airl 1")]
    [InlineData("Airlock 10", "airl 1")]
    [InlineData("Airlock Door 1", "airl 1")]
    [InlineData("Airlock Inner Door", "  air   inn  ")]
    [InlineData("Airlock Inner Door", "")]
    public void EveryWordTypedIsFound(string name, string search)
    {
        Assert.True(EntityQuery.MatchesText(name, search));
    }

    [Theory]
    [InlineData("Airlock 2", "airloc 3")]
    [InlineData("Airlock Inner Door", "door outer")]
    [InlineData("Airlock", "airlocks")]
    public void AWordNotThereIsNotFound(string name, string search)
    {
        Assert.False(EntityQuery.MatchesText(name, search));
    }

    [Theory]
    [InlineData("1 Airlock", "airl 1")]
    [InlineData("Airlock Inner Door", "door inner")]
    [InlineData("Airlock Inner Door", "inn air")]
    public void WordsOutOfOrderAreNotFound(string name, string search)
    {
        Assert.False(EntityQuery.MatchesText(name, search));
    }

    [Fact]
    public void AWordCanStartOnlyWhereTheOneBeforeItEnded()
    {
        Assert.False(EntityQuery.MatchesText("Airlock", "airlock lock"));
        Assert.True(EntityQuery.MatchesText("Airlock Lock", "airlock lock"));
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
