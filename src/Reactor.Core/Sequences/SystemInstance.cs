namespace Reactor.Sequences
{
    /// <summary>
    /// One of a system, actually fitted to the ship.
    /// </summary>
    /// <remarks>
    /// A system is what a machine is; this is one somebody has. The front airlock and the port
    /// airlock are two of these over one system, and fixing the system fixes both.
    ///
    /// The player never meets the word. It is called <c>Airlock 1</c> until they rename it,
    /// the way the game itself calls a block <c>Interior Light 4</c> - provisional enough to
    /// invite a better name without a dialog asking for one, and it never collides with the
    /// name of the system it belongs to.
    ///
    /// It holds nothing but a name so far. What it is for is the blocks: each of these will
    /// point the system's roles at its own doors and its own vent, which is why one system can
    /// run two airlocks at opposite ends of a ship.
    /// </remarks>
    public sealed class SystemInstance
    {
        public SystemInstance(int number)
        {
            Number = number;
            Name = "";
        }

        /// <summary>Which one of its system this is.</summary>
        public int Number { get; private set; }

        /// <summary>
        /// What the player called it, or empty if they have not said.
        /// </summary>
        /// <remarks>
        /// Empty is the ordinary state. The name shown is worked out from the system and this
        /// number - Airlock 1 - so a machine that only learns its own name later takes its
        /// children with it rather than leaving them called something stale.
        /// </remarks>
        public string Name { get; set; }

        public bool HasName { get { return !string.IsNullOrEmpty(Name); } }
    }
}
