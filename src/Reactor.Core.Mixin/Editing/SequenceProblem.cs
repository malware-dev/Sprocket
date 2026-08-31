namespace Reactor.Editing
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
        MissingGate
    }
}
