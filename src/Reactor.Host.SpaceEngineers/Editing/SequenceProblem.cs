namespace Reactor.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// Why a sequence cannot work.
    /// </summary>
    /// <remarks>
    /// A sequence pointing at a block that got shot off must never simply do nothing: silence is
    /// indistinguishable from working, and that is precisely the failure a non-programmer has no way
    /// to diagnose. So core states which of these it is, and a target says so however it says things.
    /// </remarks>
    public enum SequenceProblem
    {
        None,

        /// <summary>It listens to something that is no longer here.</summary>
        MissingSource,

        /// <summary>It acts on something that is no longer here.</summary>
        MissingTarget,

        /// <summary>It waits on something that is no longer here.</summary>
        MissingGate,

        /// <summary>
        /// Nothing is missing and nothing will ever start it.
        /// </summary>
        /// <remarks>
        /// A different kind of wrong from the rest of these. Every other member says something the
        /// sequence names is gone; this one says the sequence is complete, correct, and unreachable -
        /// so the fix is not to mend a reference but to give it something that fires it.
        /// <para>
        /// Two ways in so far. A button whose toolbar slot is no longer Reactor's, because somebody
        /// cleared it from the terminal - which is a legitimate way to say "stop doing that", so it
        /// is reported rather than undone. And a named sequence nothing calls, which has been
        /// possible since sequences could be named and has never been reported.
        /// </para>
        /// </remarks>
        NeverTriggered,

        /// <summary>
        /// Everything it names is here, but at least one of them was found by a weaker key.
        /// </summary>
        /// <remarks>
        /// A door was rebuilt, or the ship was pasted from a blueprint. There is a candidate and it
        /// is not accepted, because accepting it silently is how the wrong door gets cycled. The
        /// sequence does not run until somebody says that is the right one.
        /// </remarks>
        Adrift
    }
}
