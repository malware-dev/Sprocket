using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>
    /// What one act of a run comes to on the blocks of the installation it runs on: each block it acts on and what it does there, or why it cannot be
    /// done.
    /// </summary>
    public sealed class CompiledAct
    {
        /// <summary>Each block the act reaches, and what it does there.</summary>
        public readonly List<ActTarget> Acts = new List<ActTarget>();

        /// <summary>Why the act cannot be done, gone or possibly swapped, or none.</summary>
        public SequenceProblem Problem;

        /// <summary>The block that is so, by its reference.</summary>
        public string Which;

        /// <summary>Why an expression the act is given has no value, or none.</summary>
        public ExpressionFailure Failure;

        /// <summary>Whether the expressions it is given were worked out as it was made, so reaching it the first time works them out no more.</summary>
        public bool JustCompiled;
    }
}
