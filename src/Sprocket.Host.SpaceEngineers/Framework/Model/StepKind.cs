
namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// What sort of line a step is.
    /// </summary>
    public enum StepKind
    {
        /// <summary>Does something to some blocks.</summary>
        Do,

        /// <summary>Sets one of its blocks' values.</summary>
        Set,

        /// <summary>Sets one of its blocks' states on or off, which reads as enabling or disabling it.</summary>
        Enable,

        /// <summary>Passes time and nothing else.</summary>
        Wait,

        /// <summary>Waits for everything started above it to finish, where waits are rows of their own.</summary>
        Await,

        /// <summary>Starts another sequence of the same system.</summary>
        Run,

        /// <summary>Stops another sequence of the same system where it is running.</summary>
        Stop,

        /// <summary>Ends the sequence it is in, saying why at its level.</summary>
        Abort,

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
