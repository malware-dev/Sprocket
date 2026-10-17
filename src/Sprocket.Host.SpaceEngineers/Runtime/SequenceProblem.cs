
namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// Why a sequence cannot work.
    /// </summary>
    public enum SequenceProblem
    {
        /// <summary>Nothing is wrong.</summary>
        None,

        /// <summary>It listens to something that is no longer here.</summary>
        MissingSource,

        /// <summary>It acts on something that is no longer here.</summary>
        MissingTarget,

        /// <summary>It tests something that is no longer here.</summary>
        MissingGate,

        /// <summary>It asks its blocks for something they do not offer.</summary>
        Unoffered,

        /// <summary>
        /// One of its steps has not been given a value it cannot do without.
        /// </summary>
        Unfilled,

        /// <summary>One of its steps has been given a value its action does not take: another kind, or too large for its type.</summary>
        /// <remarks>Found when the program is bound, and the step does nothing to a block that would not take it.</remarks>
        Mistyped,

        /// <summary>
        /// Nothing is missing and nothing will ever start it.
        /// </summary>
        /// <remarks>
        /// A button whose toolbar slot is no longer Sprocket's, or a trigger left with nothing in it on a sequence that has
        /// steps.
        /// </remarks>
        NeverTriggered,

        /// <summary>
        /// Nothing has been given the job of starting it: no trigger, and no sequence that calls it.
        /// </summary>
        /// <remarks>A warning rather than a fault, and never parked.</remarks>
        NothingStarts,

        /// <summary>A slot it uses, and may not do without, is empty in the one of its system it runs on.</summary>
        EmptySlot,

        /// <summary>
        /// Everything it names is here, but at least one of them was found by a weaker key.
        /// </summary>
        /// <remarks>The sequence does not run until somebody accepts the candidate.</remarks>
        Adrift,

        /// <summary>
        /// A step the sequence is waiting on that the blocks say will never finish.
        /// </summary>
        /// <remarks>The words come from the block.</remarks>
        Stuck,

        /// <summary>
        /// The last run gave up because something else took a block out of its hands.
        /// </summary>
        /// <remarks>A warning rather than a fault, cleared the next time the sequence runs.</remarks>
        Conflicted,

        /// <summary>One of its steps runs a sequence that starts it again, directly or through the sequences that one runs.</summary>
        /// <remarks>The system does not run. A sequence may stop itself.</remarks>
        Circular,

        /// <summary>A step asked a block that is switched off for something a step waits for.</summary>
        /// <remarks>The run ends there and says which block.</remarks>
        Unable,

        /// <summary>A block a step waits for is switched off or not working, so it is getting nowhere.</summary>
        /// <remarks>The run holds rather than ends, since the block may be switched on or mended and carry on.</remarks>
        Stalled,

        /// <summary>One of its awaits has nothing to wait for: no step since the await before it takes time.</summary>
        /// <remarks>A warning: the await passes straight on.</remarks>
        NothingToAwait,

        /// <summary>A block in one of its steps' slots lacks the screen, or other facet, the installation chose of it.</summary>
        /// <remarks>The step does nothing to that block, and the fault names it.</remarks>
        MissingFacet
    }
}
