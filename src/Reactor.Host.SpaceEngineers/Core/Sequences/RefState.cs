namespace Reactor.Host.SpaceEngineers.Core.Sequences
{
    /// <summary>
    /// How well a reference is holding.
    /// </summary>
    /// <remarks>
    /// There were two of these and the middle one is new, which is the point of the whole design.
    /// A reference is bound, adrift with a candidate to offer, or lost.
    /// </remarks>
    public enum RefState
    {
        /// <summary>The strongest key found it. Nothing to say.</summary>
        Bound,

        /// <summary>
        /// The strongest key failed and a weaker one found something.
        /// </summary>
        /// <remarks>
        /// <b>A weaker key never rebinds by itself.</b> If an id is gone and a different door now
        /// carries the old name, binding silently would cycle the wrong door on somebody's airlock.
        /// So this is a proposal a person accepts, and a sequence holding one does not run.
        /// </remarks>
        Adrift,

        /// <summary>Nothing matched.</summary>
        Lost
    }
}
