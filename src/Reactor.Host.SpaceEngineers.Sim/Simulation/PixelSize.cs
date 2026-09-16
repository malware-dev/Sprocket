namespace Reactor.Sim.Simulation
{
    /// <summary>
    /// A size in whole pixels.
    /// </summary>
    /// <remarks>
    /// Whole numbers, because this is the resolution a frame is built at and half a pixel of screen is
    /// not a thing the game has. The desktop framework's own sizes are all doubles.
    /// </remarks>
    public struct PixelSize
    {
        public PixelSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; private set; }

        public int Height { get; private set; }
    }
}
