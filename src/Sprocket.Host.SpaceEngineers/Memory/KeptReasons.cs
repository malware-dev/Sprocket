using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>How what went wrong is kept: each problem and each block's trouble by the number set on its value.</summary>
    /// <remarks>A memory an earlier build wrote holds words instead, which are read through <see cref="EarlierSpellings" />.</remarks>
    public static class KeptReasons
    {
        // The last value of each, so a number a newer build kept reads as none.
        const int LastProblem = (int)SequenceProblem.PassedOver;
        const int LastCause = (int)Cause.ItemsAlreadyThere;

        /// <summary>The problem <paramref name="number" /> or <paramref name="spelled" /> holds, or none.</summary>
        public static SequenceProblem Problem(int number, string spelled)
        {
            if (number > 0)
                return number <= LastProblem ? (SequenceProblem)number : SequenceProblem.None;

            return EarlierSpellings.ProblemOf(spelled);
        }

        /// <summary>The cause <paramref name="number" /> or <paramref name="spelled" /> holds, or none.</summary>
        public static Cause CauseOf(int number, string spelled)
        {
            if (number > 0)
                return number <= LastCause ? (Cause)number : Cause.None;

            return EarlierSpellings.CauseOf(spelled);
        }

        /// <summary><paramref name="trouble" /> as it is kept, or null for none.</summary>
        public static TroubleMemory Kept(BlockTrouble trouble)
        {
            return trouble.Exists ? new TroubleMemory { Cause = (int)trouble.Cause, About = trouble.About ?? "", Ends = trouble.Ends } : null;
        }

        /// <summary>The trouble <paramref name="kept" /> holds, or none.</summary>
        public static BlockTrouble Trouble(TroubleMemory kept)
        {
            if (kept == null)
                return BlockTrouble.None;

            var cause = CauseOf(kept.Cause, kept.Spelled);
            var about = kept.About.Length == 0 ? null : kept.About;
            return kept.Ends ? BlockTrouble.Kept(cause, about) : BlockTrouble.Waiting(cause, about);
        }

        /// <summary><paramref name="fault" /> as it is kept, or null for none.</summary>
        public static FaultMemory Kept(SequenceFault fault)
        {
            return fault.Exists
                ? new FaultMemory { Problem = (int)fault.Problem, Step = fault.Step, Which = fault.Which ?? "", Because = Kept(fault.Because) }
                : null;
        }

        /// <summary>The fault <paramref name="kept" /> holds, or none.</summary>
        public static SequenceFault Fault(FaultMemory kept)
        {
            if (kept == null)
                return SequenceFault.None;

            return new SequenceFault(Problem(kept.Problem, kept.Spelled), kept.Step, Trouble(kept.Because),
                kept.Which.Length == 0 ? null : kept.Which);
        }

        /// <summary><paramref name="halt" /> as it is kept, or null where a person switched it off.</summary>
        public static HaltMemory Kept(Halt halt)
        {
            return halt == null
                ? null
                : new HaltMemory
                {
                    Problem = (int)halt.Problem,
                    Sequence = halt.Sequence,
                    Step = halt.Step,
                    Block = halt.Block ?? "",
                    Because = Kept(halt.Because)
                };
        }

        /// <summary>The halt <paramref name="kept" /> holds, or null where a person switched it off.</summary>
        public static Halt Halt(HaltMemory kept)
        {
            return kept == null
                ? null
                : new Halt(Problem(kept.Problem, kept.Spelled), kept.Sequence, kept.Step, Trouble(kept.Because),
                    kept.Block.Length == 0 ? null : kept.Block);
        }
    }
}
