using Sprocket.Host.SpaceEngineers.World;
using Xunit;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A value handed to one of the game's own actions goes across as the kind it was written as.
/// </summary>
/// <remarks>
/// <b>Not as the kind the action's declaration carries.</b> A declaration says how many values an
/// action takes and nothing trustworthy about what they are: the game's own color setter declares
/// three strings and then refuses anything that is not an integer. Converting to what was declared
/// meant every value was discarded and the light never changed color, which is the one failure
/// nothing above this could see - the editor was right, the program was right, and nothing happened.
/// </remarks>
public class GivenTests
{
    [Fact]
    public void AWholeNumberGoesAsAThirtyTwoBitOne()
    {
        TerminalActionParameter given = GameSlots.Given(Literal.Of(255));

        Assert.Equal(255, given.Value);
        Assert.IsType<int>(given.Value);
    }

    /// <summary>
    /// Because the game's own handlers cast the boxed value to <c>int</c>.
    /// </summary>
    /// <remarks>
    /// They say they accept a 64-bit one and then cast as though it were 32, which throws. Anything
    /// that fits therefore goes as the narrower kind, and only what does not fit goes wide.
    /// </remarks>
    [Fact]
    public void OneTooBigForThatGoesWide()
    {
        TerminalActionParameter given = GameSlots.Given(Literal.Of(5000000000L));

        Assert.Equal(5000000000L, given.Value);
        Assert.IsType<long>(given.Value);
    }

    [Fact]
    public void ARealGoesAsAFloat()
    {
        TerminalActionParameter given = GameSlots.Given(Literal.Of(4.5));

        Assert.Equal(4.5f, given.Value);
        Assert.IsType<float>(given.Value);
    }

    [Fact]
    public void AFlagGoesAsABool()
    {
        Assert.Equal(true, GameSlots.Given(Literal.Of(true)).Value);
        Assert.Equal(false, GameSlots.Given(Literal.Of(false)).Value);
    }

    [Fact]
    public void TextGoesAsText()
    {
        Assert.Equal("doors open", GameSlots.Given(Literal.Of("doors open")).Value);
    }

    /// <summary>A color goes as the way it is written, the game having no value of its own for one.</summary>
    [Fact]
    public void AColorGoesAsHowItIsWritten()
    {
        Assert.Equal("#FF8800", GameSlots.Given(Literal.Color(255, 136, 0, 255)).Value);
    }
}
