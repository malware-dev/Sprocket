


namespace Sprocket.Sim.Simulation
{
    /// <summary>Runs a script with nobody watching and writes out the frame it ended on.</summary>
    public static class FrameDump
    {
        /// <summary>The frame the editor opens on, in a window of <paramref name="windowWidth" /> by <paramref name="windowHeight" />.</summary>
        public static string Render(float windowWidth, float windowHeight)
        {
            return Render(windowWidth, windowHeight, null);
        }

        /// <summary>The frame <paramref name="script" /> ends on, in a window of <paramref name="windowWidth" /> by <paramref name="windowHeight" />.</summary>
        public static string Render(float windowWidth, float windowHeight, Script script)
        {
            var frame = new Offscreen(windowWidth, windowHeight);
            frame.Play(script);

            return frame.Describe();
        }
    }
}
