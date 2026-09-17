namespace Reactor.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// Turns what a command is for, and which one it is, into an id.
    /// </summary>
    /// <remarks>
    /// Packed rather than hashed, so it is exact: no two different commands can ever collide
    /// by accident, and an id read in a debugger says what it is. Eight bits of purpose and
    /// two twelve-bit numbers is enough for four thousand steps on a page, which is four
    /// thousand more than anybody will write.
    ///
    /// Uniqueness only has to hold within one screen. A rebuild clears the table and registers
    /// one page's worth, and only one screen is ever live.
    /// </remarks>
    static class CommandId
    {
        const int Slots = 4096;

        public static int Of(Cmd kind)
        {
            return Of(kind, 0, 0);
        }

        public static int Of(Cmd kind, int which)
        {
            return Of(kind, which, 0);
        }

        /// <summary>
        /// <paramref name="which" /> and <paramref name="part" /> are <b>ordinals, never identifiers</b>.
        /// </summary>
        /// <remarks>
        /// Twelve bits each, and out of range is clamped rather than refused, which is right for a count of
        /// steps and quietly catastrophic for an id. A block's id belongs to the game and arrives in the
        /// quintillions, so passing one here collapsed every block on a real ship onto the same number.
        /// Give an entity a small number of your own and pass that; <c>PickPage.Slot</c> is the pattern.
        /// </remarks>
        public static int Of(Cmd kind, int which, int part)
        {
            // Clamped rather than checked. A page with four thousand steps is not a case worth
            // throwing over, and two commands colliding at the far end of that is caught by the
            // duplicate check where every other collision is.
            if (which < 0 || which >= Slots)
                which = Slots - 1;

            if (part < 0 || part >= Slots)
                part = Slots - 1;

            return ((int)kind << 24) | (which << 12) | part;
        }

        /// <summary>What sort of command an id is, which is the byte it was built from.</summary>
        public static Cmd Kind(int id)
        {
            return (Cmd)(id >> 24);
        }
    }
}
