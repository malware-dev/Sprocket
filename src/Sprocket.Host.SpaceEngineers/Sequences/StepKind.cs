
namespace Sprocket.Host.SpaceEngineers.Sequences
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

        /// <summary>Sets one of its blocks' values.</summary>
        Set,

        /// <summary>Passes time and nothing else.</summary>
        Wait,

        /// <summary>Starts another sequence of the same system.</summary>
        Run,

        /// <summary>Stops another sequence of the same system where it is running.</summary>
        Stop,

        /// <summary>Opens an if: runs the steps under it where its test holds.</summary>
        If,

        /// <summary>The next branch of an if, tested where nothing before it held.</summary>
        OtherwiseIf,

        /// <summary>The last branch of an if, run where nothing before it held.</summary>
        Otherwise,

        /// <summary>Closes an if; where its branches are added.</summary>
        End,

        /// <summary>The end of a branch of an if, where steps are added to it; no step of its own.</summary>
        Adds
    }
}
