using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A block's words, written for one block, said for several joined with "and": the first word made plural.</summary>
public class PluralTests
{
    /// <summary>A verb is made plural by its ending, and the words after it stay.</summary>
    [Theory]
    [InlineData("finishes closing", "finish closing")]
    [InlineData("is open", "are open")]
    [InlineData("is not open", "are not open")]
    [InlineData("has ice", "have ice")]
    [InlineData("becomes empty", "become empty")]
    [InlineData("runs dry", "run dry")]
    [InlineData("detaches", "detach")]
    [InlineData("passes", "pass")]
    [InlineData("empties", "empty")]
    [InlineData("merges", "merge")]
    [InlineData("press", "press")]
    public void TheFirstWordTakesThePlural(string one, string several)
    {
        Assert.Equal(several, SentenceText.Plural(one));
    }
}
