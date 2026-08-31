using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;


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
