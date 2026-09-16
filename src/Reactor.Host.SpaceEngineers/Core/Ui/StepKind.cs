namespace Reactor.Host.SpaceEngineers.Core.Ui
{
    /// <summary>
    /// What sort of line a step is.
    /// </summary>
    /// <remarks>
    /// Waiting a set time is not an action on a block, and pretending otherwise is what made the flow
    /// awkward - so it is its own kind of line rather than a step with the verb left out.
    /// </remarks>
    public enum StepKind
    {
        /// <summary>Does something to some blocks.</summary>
        Do,

        /// <summary>Passes time and nothing else.</summary>
        Wait,

        /// <summary>Starts another sequence of the same system.</summary>
        Run
    }
}
