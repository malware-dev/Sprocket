using Sprocket.Host.SpaceEngineers.Framework.Game.Members;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// A step that spends time and does nothing else.
    /// </summary>
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

        /// <summary>How long to wait, as the script writes it, where <see cref="Setting" /> is empty.</summary>
        public Duration Length { get; set; }

        /// <summary>The setting whose length of time it waits, each installation's own, or empty where it waits <see cref="Length" />.</summary>
        public string Setting { get; set; } = "";

        /// <summary>How long to wait, in the game's own ticks, where it waits <see cref="Length" />.</summary>
        public int Ticks => Length.Ticks;

        /// <summary>How long to wait in <paramref name="through" />, in ticks: <see cref="Length" />, or the length its setting has there; none where that setting has no length.</summary>
        /// <remarks>Asked as the step is reached, never on a tick.</remarks>
        public int TicksIn(SystemInstance through)
        {
            if (Setting.Length == 0)
                return Ticks;

            Literal value;
            return through != null && through.Gives(Setting, out value) && value.Kind == ValueKind.Duration
                ? value.Length.Ticks
                : 0;
        }
    }
}
