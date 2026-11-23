using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// What a branch of an if tests: a member read on blocks, a <see cref="Comparison" />, one of the logic system's states, a
    /// <see cref="StateTest" />, whether the host has a namespace, a <see cref="HostTest" />, or an expression giving a flag, an
    /// <see cref="ExpressionTest" />.
    /// </summary>
    public abstract class BranchTest
    {
        static readonly string[] Nothing = new string[0];

        /// <summary>The names of the blocks it reads: a system's variables, or a system blueprint's slots; none where it reads no blocks.</summary>
        public virtual IList<string> Targets => Nothing;
    }
}
