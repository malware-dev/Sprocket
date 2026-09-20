using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// The seam between a colour and the separate numbers an action takes it as.
/// </summary>
/// <remarks>
/// Both halves of it, because they are only right together: what a step holds has to come back out
/// of the picker as the same step. A colour that survived being shown and did not survive being put
/// back would quietly rewrite every light somebody opened and did not change.
/// </remarks>
public class ColourTests
{
    [Fact]
    public void ThreeNumbersAreShownAsTheColourTheyMake()
    {
        Assert.Equal("#FF8800", Channels.Said(Values(255, 136, 0), 0, 3));
    }

    [Fact]
    public void AndTheColourGoesBackAsThoseThreeNumbers()
    {
        var into = new List<string>();
        Channels.Spell("#FF8800", 3, into);

        Assert.Equal(new[] { "255", "136", "0" }, into);
    }

    /// <summary>
    /// A written colour survives being shown and put back unchanged.
    /// </summary>
    /// <remarks>
    /// The one thing that has to hold, because opening a step and pressing Continue without touching
    /// anything is a thing people do, and it must leave the step as it was.
    /// </remarks>
    [Fact]
    public void AndComesBackTheSameWayItWentIn()
    {
        var into = new List<string>();
        Channels.Spell(Channels.Said(Values(18, 52, 86), 0, 3), 3, into);

        Assert.Equal(new[] { "18", "52", "86" }, into);
    }

    /// <summary>Values nobody has filled in are no colour at all, rather than black.</summary>
    /// <remarks>
    /// A step somebody has not finished writing has not chosen black. Opening its picker on black
    /// would be an answer it never gave, and pressing Continue would then write one.
    /// </remarks>
    [Fact]
    public void ValuesThatWereNeverGivenAreNoColour()
    {
        Assert.Equal("", Channels.Said(new List<Literal>(), 0, 3));
        Assert.Equal("", Channels.Said(Values(255, 136, 0), 1, 3));
    }

    /// <summary>And a value that is not a number is not a channel of anything.</summary>
    [Fact]
    public void NorIsAnythingThatIsNotANumber()
    {
        var said = new List<Literal> { Literal.Of("red"), Literal.Of(136L), Literal.Of(0L) };

        Assert.Equal("", Channels.Said(said, 0, 3));
    }

    /// <summary>What is not a colour yet leaves the values alone rather than inventing them.</summary>
    [Fact]
    public void HalfATypedColourFillsNothingIn()
    {
        var into = new List<string>();
        Channels.Spell("#FF88", 3, into);

        Assert.Equal(new[] { "", "", "" }, into);
    }

    /// <summary>
    /// How solid it is has no channel of its own, because nothing that takes three wants it.
    /// </summary>
    [Fact]
    public void AndHowSolidItIsIsNotOneOfTheThree()
    {
        var into = new List<string>();
        Channels.Spell("#FF8800CC", 3, into);

        Assert.Equal(new[] { "255", "136", "0" }, into);
    }

    /// <summary>
    /// A colour that is one value stays one value, written the way the language writes one.
    /// </summary>
    /// <remarks>
    /// The case with no conversion in it at all. An action that takes a colour as a colour is handed
    /// what somebody chose, and the picker is the same picker.
    /// </remarks>
    [Fact]
    public void AColourThatIsOneValueGoesStraightThrough()
    {
        var kept = new List<Literal> { Literal.Colour(255, 136, 0, 255) };
        string said = Channels.Said(kept, 0, 1);

        var into = new List<string>();
        Channels.Spell(said, 1, into);

        Assert.Equal(new[] { said }, into);
    }

    static List<Literal> Values(params long[] wholes)
    {
        var said = new List<Literal>();
        foreach (long whole in wholes)
            said.Add(Literal.Of(whole));

        return said;
    }
}
