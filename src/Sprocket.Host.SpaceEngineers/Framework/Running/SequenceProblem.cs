
namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>
    /// Why a sequence cannot work.
    /// </summary>
    /// <remarks>Persisted: each value's number is kept in saves, so a number is never changed or given to another value.</remarks>
    public enum SequenceProblem
    {
        /// <summary>Nothing is wrong.</summary>
        None = 0,

        /// <summary>It listens to something that is no longer here.</summary>
        MissingSource = 1,

        /// <summary>It acts on something that is no longer here.</summary>
        MissingTarget = 2,

        /// <summary>It tests something that is no longer here.</summary>
        MissingGate = 3,

        /// <summary>It asks its blocks for something they do not offer.</summary>
        Unoffered = 4,

        /// <summary>
        /// One of its steps has not been given a value it cannot do without.
        /// </summary>
        Unfilled = 5,

        /// <summary>One of its steps has been given a value its action does not take: another kind, or too large for its type.</summary>
        /// <remarks>Found when the program is bound, and the step does nothing to a block that would not take it.</remarks>
        Mistyped = 6,

        /// <summary>
        /// Nothing is missing and nothing will ever start it.
        /// </summary>
        /// <remarks>
        /// A button whose toolbar slot is no longer Sprocket's, or a trigger left with nothing in it on a sequence that has
        /// steps.
        /// </remarks>
        NeverTriggered = 7,

        /// <summary>
        /// Nothing has been given the job of starting it: no trigger, and no sequence that calls it.
        /// </summary>
        /// <remarks>A warning rather than a fault, and never parked.</remarks>
        NothingStarts = 8,

        /// <summary>A slot it uses, and may not do without, is empty in the one of its system it runs on.</summary>
        EmptySlot = 9,

        /// <summary>
        /// Everything it names is here, but at least one of them was found by a weaker key.
        /// </summary>
        /// <remarks>The sequence does not run until somebody accepts the candidate.</remarks>
        Adrift = 10,

        /// <summary>
        /// A step the sequence is waiting on that the blocks say will never finish.
        /// </summary>
        /// <remarks>The words come from the block.</remarks>
        Stuck = 11,

        /// <summary>
        /// The last run gave up because something else took a block out of its hands.
        /// </summary>
        /// <remarks>A warning rather than a fault, cleared the next time the sequence runs.</remarks>
        Conflicted = 12,

        /// <summary>One of its steps runs a sequence that starts it again, directly or through the sequences that one runs.</summary>
        /// <remarks>The system does not run. A sequence may stop itself.</remarks>
        Circular = 13,

        /// <summary>A step asked a block that is switched off for something a step waits for.</summary>
        /// <remarks>The run ends there and says which block.</remarks>
        Unable = 14,

        /// <summary>A block a step waits for is switched off or not working, so it is getting nowhere.</summary>
        /// <remarks>The run holds rather than ends, since the block may be switched on or mended and carry on.</remarks>
        Stalled = 15,

        /// <summary>One of its awaits has nothing to wait for: no step since the await before it takes time.</summary>
        /// <remarks>A warning: the await passes straight on.</remarks>
        NothingToAwait = 16,

        /// <summary>A block in one of its steps' slots lacks the screen, or other facet, the installation chose of it.</summary>
        /// <remarks>The step does nothing to that block, and the fault names it.</remarks>
        MissingFacet = 17,

        /// <summary>One of its steps does something to a screen, on a slot that holds blocks rather than screens.</summary>
        /// <remarks>A warning: the step still runs, and giving it a screen slot in the picker mends it.</remarks>
        ScreenOnBlockSlot = 18,

        /// <summary>One of its if steps takes the average of a set that holds no blocks, so it has no number to compare.</summary>
        /// <remarks>The run ends there.</remarks>
        NothingToAverage = 19,

        /// <summary>A block a step or a trigger reads can't answer what it is asked, so it is left out.</summary>
        PassedOver = 20,

        /// <summary>An abort step ended the run, as a note; the fault's words are its message.</summary>
        Aborted = 21,

        /// <summary>An abort step ended the run, as a warning; the fault's words are its message.</summary>
        AbortedWarning = 22,

        /// <summary>An abort step ended the run, as a problem; the fault's words are its message.</summary>
        /// <remarks>The installation the run was on is switched off, with the message as the reason.</remarks>
        AbortedProblem = 23,

        /// <summary>
        /// A name one of its steps, tests or triggers writes could mean more than one thing, so it is not told which is meant: a
        /// setting named alone that its member also takes as one of its words, or a member named alone that more than one
        /// namespace, the game's included, offers on its blocks.
        /// </summary>
        /// <remarks>Found when the program is bound; the fault's words are the name as written, and the name written with more detail mends it.</remarks>
        Ambiguous = 24,

        /// <summary>The last run ended because a mod's awaitable action failed.</summary>
        /// <remarks>A problem a sequence's catch handles; the words come from the mod.</remarks>
        Failed = 25
    }
}
