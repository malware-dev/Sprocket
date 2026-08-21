using System.Collections.Generic;

namespace Reactor.Editing
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

    /// <summary>
    /// The rates offered for "Every ___".
    /// </summary>
    /// <remarks>
    /// Reactor measures time in <em>ticks</em> - one tick is one step of the game's
    /// simulation. This is the only honest unit available: simulation speed sags
    /// under load, so anything promising seconds would quietly drift exactly when the
    /// user's machine matters most.
    ///
    /// "Tick" is not explained anywhere in the UI, because it does not have to be. A
    /// repeating sequence shows an indicator pulsing at its true rate, so the user
    /// learns what a tick is by watching one - and when the server bogs down, the
    /// pulse slows along with the machine instead of lying about it.
    ///
    /// The values are not arbitrary. One, ten and a hundred are what the game itself
    /// offers; the rest are round multiples of a hundred so a host can reach them by
    /// counting cheaply.
    /// </remarks>
    public static class Cadences
    {
        /// <summary>
        /// What a new repeating sequence starts at: native, and slow enough to be
        /// harmless if the user never touches it.
        /// </summary>
        public const int Default = 100;

        static readonly Cadence[] _all =
        {
            new Cadence("every tick", 1, true,
                "Runs sixty times a second. Hard on the server - only worth it if nothing slower will do."),
            new Cadence("fast", 10, true,
                "Six times a second. Still costly; try something slower first."),
            new Cadence("steady", 100, true),
            new Cadence("slow", 300, false),
            new Cadence("very slow", 600, false),
            new Cadence("occasional", 1800, false),
            new Cadence("rare", 3600, false)
        };

        public static IList<Cadence> All { get { return _all; } }

        /// <summary>The offered rate nearest a raw tick count, for display.</summary>
        public static Cadence Nearest(int ticks)
        {
            var best = _all[0];
            var bestDistance = int.MaxValue;
            for (var i = 0; i < _all.Length; i++)
            {
                var distance = _all[i].Ticks - ticks;
                if (distance < 0)
                    distance = -distance;
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = _all[i];
            }
            return best;
        }
    }
}
