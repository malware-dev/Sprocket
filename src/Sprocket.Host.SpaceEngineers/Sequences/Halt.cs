using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Why Sprocket switched off a logic system, or one of a blueprint's, that could not work, as it knew it then.</summary>
    public sealed class Halt
    {
        /// <summary>A halt for <paramref name="problem" /> in sequence <paramref name="sequence" /> at <paramref name="step" />, naming the block at fault and what it said was wrong where it said anything.</summary>
        public Halt(SequenceProblem problem, int sequence, int step, BlockTrouble because, string block)
        {
            Problem = problem;
            Sequence = sequence;
            Step = step;
            Because = because;
            Block = block;
        }

        /// <summary>What was wrong, or <see cref="SequenceProblem.None" /> where that is not known.</summary>
        public SequenceProblem Problem { get; private set; }

        /// <summary>The number of the sequence at fault, or 0 where that is not known.</summary>
        public int Sequence { get; private set; }

        /// <summary>Which step was at fault, as an index into the steps, or -1 for what starts it.</summary>
        public int Step { get; private set; }

        /// <summary>What the block at fault said was wrong, or none where it said nothing.</summary>
        public BlockTrouble Because { get; private set; }

        /// <summary>The name the block at fault was last known by on the construct, or null where no one block was.</summary>
        public string Block { get; private set; }
    }
}
