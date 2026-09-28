using Sprocket.Host.SpaceEngineers.Features.Picking;
using Xunit;

namespace Sprocket.Tests;

/// <summary>What typing into a search box finds: each word on its own, in any order, whatever its case.</summary>
public class SearchTests
{
    [Theory]
    [InlineData("Airlock 2", "airloc 2")]
    [InlineData("Airlock 2", "AIRLOCK")]
    [InlineData("Airlock Inner Door", "door inner")]
    [InlineData("Airlock Inner Door", "  inn   air  ")]
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
}
