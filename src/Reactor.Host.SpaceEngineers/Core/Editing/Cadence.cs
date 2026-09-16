
namespace Reactor.Host.SpaceEngineers.Core.Editing
{
    /// <summary>
    /// A named repeat rate, so the user dials a rhythm instead of typing a number.
    /// </summary>
    public sealed class Cadence
    {
        public Cadence(string name, int ticks, bool isNative, string warning = null)
        {
            Name = name;
            Ticks = ticks;
            IsNative = isNative;
            Warning = warning ?? "";
        }

        public string Name { get; private set; }

        public int Ticks { get; private set; }

        /// <summary>
        /// Whether the game wakes a mod at this rate directly.
        /// </summary>
        /// <remarks>
        /// Space Engineers offers exactly three: every frame, every ten, every
        /// hundred. Everything else is a host running at the nearest native rate and
        /// counting down, which is affordable for multiples of a hundred and less so
        /// below that. Not shown to the user - it is a fact about the host, not about
        /// their machine - but it is why the list looks the way it does.
        /// </remarks>
        public bool IsNative { get; private set; }

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
