using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// The rates offered for "Every ___".
    /// </summary>
    /// <remarks>
    /// Sprocket measures time in <em>ticks</em> - one tick is one step of the game's
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

        public static readonly IReadOnlyList<Cadence> All = new[]
        {
            new Cadence("every tick", 1,
                "Runs sixty times a second. Hard on the server - only worth it if nothing slower will do."),
            new Cadence("fast", 10,
                "Six times a second. Still costly; try something slower first."),
            new Cadence("steady", 100),
            new Cadence("slow", 300),
            new Cadence("very slow", 600),
            new Cadence("occasional", 1800),
            new Cadence("rare", 3600)
        };
    }
}
