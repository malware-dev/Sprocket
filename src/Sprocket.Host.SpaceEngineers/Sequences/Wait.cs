
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
        /// <summary>A wait of <paramref name="ticks" /> ticks, or none for fewer than none.</summary>
        public Wait(int ticks) : this(Duration.OfTicks(ticks < 0 ? 0 : ticks))
        {
        }

        /// <summary>A wait as long as <paramref name="length" />.</summary>
        public Wait(Duration length)
        {
            Length = length;
        }

        /// <summary>How long to wait, as the script writes it.</summary>
        public Duration Length { get; set; }

        /// <summary>How long to wait, in the game's own ticks.</summary>
        public int Ticks => Length.Ticks;
    }
}
