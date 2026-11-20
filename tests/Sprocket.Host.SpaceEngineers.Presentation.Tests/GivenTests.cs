using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using Sprocket.Host.SpaceEngineers.Framework.Model;
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
    /// <summary>An integer goes as a 32-bit one, a real as a float, a flag as a bool, text as text and a color as the way it is written, the game having no value of its own for one; only a number too big for 32 bits goes wide.</summary>
    [Fact]
    public void AValueGoesAsTheKindItWasWrittenAs()
    {
        TerminalActionParameter whole = GameSlots.Given(Literal.Of(255));

        Assert.Equal(255, whole.Value);
        Assert.IsType<int>(whole.Value);

        // The game's own handlers cast the boxed value to int. They say they accept a 64-bit one and then cast as though
        // it were 32, which throws. Anything that fits therefore goes as the narrower kind, and only what does not fit goes wide.
        TerminalActionParameter wide = GameSlots.Given(Literal.Of(5000000000L));

        Assert.Equal(5000000000L, wide.Value);
        Assert.IsType<long>(wide.Value);

        TerminalActionParameter real = GameSlots.Given(Literal.Of(4.5));

        Assert.Equal(4.5f, real.Value);
        Assert.IsType<float>(real.Value);

        Assert.Equal(true, GameSlots.Given(Literal.Of(true)).Value);
        Assert.Equal(false, GameSlots.Given(Literal.Of(false)).Value);

        Assert.Equal("doors open", GameSlots.Given(Literal.Of("doors open")).Value);

        Assert.Equal("#FF8800", GameSlots.Given(Literal.Color(255, 136, 0, 255)).Value);
    }
}
