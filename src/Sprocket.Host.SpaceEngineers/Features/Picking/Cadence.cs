

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// A named repeat rate, so the user dials a rhythm instead of typing a number.
    /// </summary>
    public sealed class Cadence
    {
        public Cadence(string name, int ticks, string warning = null)
        {
            Name = name;
            Ticks = ticks;
            Warning = warning ?? "";
        }

        public string Name { get; private set; }

        public int Ticks { get; private set; }

        /// <summary>
        /// What it costs to pick this, in plain words, or empty.
        /// </summary>
        /// <remarks>
        /// The fast rates are genuinely expensive, and the user has no way to know
        /// that. Finding out by watching the server stutter is not a fair way to
        /// learn it.
        /// </remarks>
        public string Warning { get; private set; }
    }
}
