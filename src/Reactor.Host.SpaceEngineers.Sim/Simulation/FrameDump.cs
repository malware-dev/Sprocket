using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;


namespace Reactor.Sim.Simulation
{
    /// <summary>Runs a script with nobody watching and writes out the frame it ended on.</summary>
    public static class FrameDump
    {
        public static string Render(float windowWidth, float windowHeight)
        {
            return Render(windowWidth, windowHeight, null);
        }

        public static string Render(float windowWidth, float windowHeight, Script script)
        {
            var frame = new Offscreen(windowWidth, windowHeight);
            frame.Play(script);

            return frame.Describe();
        }
    }
}
