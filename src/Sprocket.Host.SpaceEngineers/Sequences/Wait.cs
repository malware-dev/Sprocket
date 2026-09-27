
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A step that spends time and does nothing else.
    /// </summary>
    /// <remarks>
    /// For the things nothing can report the end of - a beacon flashing, a vent that has no signal
    /// worth waiting on - and for a deliberate pause between two acts.
    /// </remarks>
    public sealed class Wait : Step
    {
        public Wait(int ticks)
        {
            Ticks = ticks < 0 ? 0 : ticks;
        }

        /// <summary>How long to wait, in the game's own ticks.</summary>
        public int Ticks { get; set; }
    }
}
