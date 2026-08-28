namespace Reactor.Editing
{
    /// <summary>
    /// What a command is for. Half of a command's identity; the rest is which one it is.
    /// </summary>
    /// <remarks>
    /// A command id used to be a counter reset on every rebuild, so it meant "the seventh thing
    /// this page registered this time round" - a position, not a reference. It was stable only
    /// by coincidence, when nothing about the page's shape had changed, and nothing enforced or
    /// noticed when that stopped being true. Every id captured by a target went quietly dead
    /// the moment anything moved, and <see cref="ReactorEditor.Invoke"/> ignores an id it does
    /// not know, so it failed invisibly.
    ///
    /// We had already answered this question everywhere else - R47 says references are by
    /// identity, and an edit carries what it points at - so this was the last place still
    /// asking "which slot was it in".
    /// </remarks>
    enum Cmd
    {
        None = 0,

        // Screen furniture. One of each per screen.
        Back,
        Primary,
        Rename,
        Switch,

        // The list screens.
        NewSystem,
        LoadSystem,
        OpenSystem,
        AddInstance,
        StartWhen,
        StartEvery,
        OpenSequence,

        // A sequence.
        Enable,
        EnableInstance,
        DeleteSequence,
        Undo,
        Redo,
        AddAction,
        AddWait,
        EditTrigger,
        EditCadence,
        TriggerSlot,
        StepSlot,
        MoveStep,
        DeleteStep,

        // Picking.
        PickEntity,
        PickChoice,
        NarrowEntities,
        NarrowChoices,
        AnyKind,
        OneKind,
        RequireAny,
        RequireAll,

        // A list of answers.
        Choice
    }

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
    }
}
