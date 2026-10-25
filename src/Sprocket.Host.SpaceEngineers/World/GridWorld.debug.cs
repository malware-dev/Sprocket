namespace Sprocket.Host.SpaceEngineers.World
{
    // How often the construct is read, counted for the harness, which proves a quiet tick reads nothing. A .debug.cs file,
    // compiled into local builds only and never packed for the game.
    public sealed partial class GridWorld
    {
        /// <summary>How many times the construct has been read, by anything.</summary>
        public int Reads { get; private set; }

        partial void Read()
        {
            Reads++;
        }
    }
}
