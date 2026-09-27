
namespace Sprocket.Sim.Machine;

/// <summary>
/// The harness's own frame counter, which is what the game's session reports.
/// </summary>
/// <remarks>
/// The mod asks the game how many frames have passed wherever it measures how long something has
/// been the way it is - a vent moving no gas, a rotor at the same angle. The harness answers that
/// question here so those measurements can be driven by a test rather than by a clock.
/// <para>
/// Static because the thing it stands in for is: <c>MyAPIGateway.Session</c> is one session for the
/// process, and the two stand-ins built over it - the rig's and the screen device's - have to report
/// the same frame.
/// </para>
/// </remarks>
static class Clock
{
    /// <summary>How many frames the harness has run.</summary>
    public static int Frame { get; private set; }

    /// <summary>One frame on, which a rig does as it ticks.</summary>
    public static void Tick()
    {
        Frame++;
    }
}
