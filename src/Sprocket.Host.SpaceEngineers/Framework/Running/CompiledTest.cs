using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>
    /// What one test of a run comes to on the blocks of the installation it runs on: each block it reads, or why it cannot be read.
    /// </summary>
    public sealed class CompiledTest
    {
        /// <summary>Each block the test reads.</summary>
        public readonly List<Probe> Reads = new List<Probe>();

        /// <summary>Why the test cannot be read, a slot empty or a block gone or possibly swapped, or none.</summary>
        public SequenceProblem Problem;

        /// <summary>The slot or block that is so, by its reference.</summary>
        public string Which;

        /// <summary>What the member is compared with on this installation: the test's value, or the value it gives the setting standing there.</summary>
        public Literal Value;
    }
}
