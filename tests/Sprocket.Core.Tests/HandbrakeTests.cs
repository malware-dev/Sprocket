using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A seat's handbrake and parking, whose game setter flips the grid's switch whatever it is told, set by reading first.
/// </summary>
/// <remarks>Through the game's own terminal floor, over stand-ins for a seat and its switch, since the rig answers values its own way.</remarks>
public class HandbrakeTests
{
    /// <summary>A switch already as asked is left alone, and one that is not is set once.</summary>
    [Theory]
    [InlineData("HandBrake", false, true, 1)]
    [InlineData("HandBrake", true, true, 0)]
    [InlineData("Park", true, false, 1)]
    [InlineData("Park", false, false, 0)]
    public void ASwitchThatFlipsIsSetOnlyWhereItDiffers(string id, bool was, bool asked, int flips)
    {
        var state = was;
        var flipped = 0;
        var property = Pretend.To<ITerminalProperty<bool>>()
            .Gives("Id", () => id)
            .Gives("TypeName", () => "Boolean")
            .Does("GetValue", _ => state)
            .Does("SetValue", _ =>
            {
                state = !state;
                flipped++;
            })
            .Done();
        var seat = Pretend.To<IMyCockpit>().Does("GetProperty", _ => property).Done();

        Assert.True(new GameSlots().Set(seat, id, Literal.Of(asked)));

        Assert.Equal(flips, flipped);
        Assert.Equal(asked, state);
    }
}
