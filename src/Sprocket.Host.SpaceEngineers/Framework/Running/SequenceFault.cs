using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using Sprocket.Host.SpaceEngineers.Framework.Language.Format.Repairs;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>
    /// What is wrong with a sequence, and which line of it is wrong.
    /// </summary>
    /// <remarks>One fault at a time: the next reports itself once the first is fixed.</remarks>
    public struct SequenceFault
    {
        /// <summary>What <see cref="Step" /> holds when the line at fault is the one that starts it.</summary>
        public const int Trigger = -1;

        /// <summary>Nothing wrong.</summary>
        public static readonly SequenceFault None = new SequenceFault(SequenceProblem.None, Trigger);

        /// <summary>Nothing can start the sequence, for <paramref name="why" />, on the button of <paramref name="block" /> where one is known.</summary>
        public static SequenceFault Unstarted(Unwired why, string block)
        {
            var fault = new SequenceFault(SequenceProblem.NeverTriggered, Trigger, BlockTrouble.None, block);
            fault.Unwired = why;
            return fault;
        }

        /// <summary><paramref name="problem" />, at the line <paramref name="step" />, with nothing said by a block.</summary>
        public SequenceFault(SequenceProblem problem, int step) : this(problem, step, BlockTrouble.None)
        {
        }

        /// <summary><paramref name="problem" />, at the line <paramref name="step" />, with what the block at fault said is wrong in <paramref name="because" />.</summary>
        public SequenceFault(SequenceProblem problem, int step, BlockTrouble because) : this()
        {
            Problem = problem;
            Step = step;
            Because = because;
        }

        /// <summary><paramref name="problem" />, at the line <paramref name="step" />, with what the block at fault said in <paramref name="because" />, naming that block by <paramref name="which" />, its reference.</summary>
        public SequenceFault(SequenceProblem problem, int step, BlockTrouble because, string which) : this(problem, step, because)
        {
            Which = which;
        }

        /// <summary>The reference of the one block at fault, or null where no one block is; for a trigger nothing can start, the name of the block whose button it is.</summary>
        public string Which { get; private set; }

        /// <summary>Why nothing can start the sequence, for a <see cref="SequenceProblem.NeverTriggered" /> fault.</summary>
        public Unwired Unwired { get; private set; }

        /// <summary>What the block at fault said is wrong, or none where it said nothing.</summary>
        public BlockTrouble Because { get; private set; }

        /// <summary>What is wrong.</summary>
        public SequenceProblem Problem { get; private set; }

        /// <summary>
        /// Which step is at fault, or <see cref="Trigger" /> for the line that starts it.
        /// </summary>
        /// <remarks>Says nothing while <see cref="Exists" /> is false.</remarks>
        public int Step { get; private set; }

        /// <summary>Whether there is anything wrong at all.</summary>
        public bool Exists => Problem != SequenceProblem.None;

        /// <summary>Whether this only says how the sequence is set up, which is fine as it is: shown on it, and counted nowhere.</summary>
        public bool IsNote => Problem == SequenceProblem.NothingStarts || Problem == SequenceProblem.Aborted;

        /// <summary>
        /// Whether this is worth saying quietly rather than marking as broken.
        /// </summary>
        /// <remarks>A warning is not counted as wrong and never has the sequence parked.</remarks>
        public bool IsWarning => Problem == SequenceProblem.NothingStarts
            || Problem == SequenceProblem.PassedOver
            || Problem == SequenceProblem.Conflicted
            || Problem == SequenceProblem.NothingToAwait
            || Problem == SequenceProblem.Aborted
            || Problem == SequenceProblem.AbortedWarning
            || ProgramRepairs.Of(Problem) != null;

        /// <summary>Whether it is wrong in the logic system itself, the same in every installation, rather than in what one installation holds.</summary>
        public bool IsAboutSystem => (ProgramRepairs.Of(Problem)?.IsAboutSystem ?? false)
            || Problem == SequenceProblem.Circular
            || Problem == SequenceProblem.NothingStarts
            || Problem == SequenceProblem.NothingToAwait;

        /// <summary>
        /// Whether this is reason to switch the sequence off.
        /// </summary>
        /// <remarks>A fault that holds a run mid-way, stuck or stalled, is no reason; the world may mend it. Neither is one that waits.</remarks>
        public bool Parks => Exists && !IsWarning && !Waits && Problem != SequenceProblem.Stuck && Problem != SequenceProblem.Stalled;

        /// <summary>Whether the sequence starts nothing until its installation is filled in, an empty slot, rather than being switched off.</summary>
        public bool Waits => Problem == SequenceProblem.EmptySlot;

        /// <summary>Whether <paramref name="other" /> says the same thing at the same line.</summary>
        public bool Same(SequenceFault other)
        {
            return Problem == other.Problem && Step == other.Step && Which == other.Which && Unwired == other.Unwired
                && Because.Same(other.Because);
        }
    }
}
