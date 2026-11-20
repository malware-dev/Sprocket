using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>How what went wrong is stored: each problem and each block's trouble by the number set on its value.</summary>
    /// <remarks>A memory an earlier build wrote holds words instead, which are read through <see cref="EarlierSpellings" />.</remarks>
    public static class StoredReasons
    {
        // The last value of each, so a number a newer build stored reads as none.
        const int LastProblem = (int)SequenceProblem.Ambiguous;
        const int LastCause = (int)Cause.PistonHeld;

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

        /// <summary><paramref name="trouble" /> as it is stored, or null for none.</summary>
        public static TroubleMemory Stored(BlockTrouble trouble)
        {
            return trouble.Exists ? new TroubleMemory { Cause = (int)trouble.Cause, About = trouble.About ?? "", Ends = trouble.Ends } : null;
        }

        /// <summary>The trouble <paramref name="stored" /> holds, or none.</summary>
        public static BlockTrouble Trouble(TroubleMemory stored)
        {
            if (stored == null)
                return BlockTrouble.None;

            var cause = CauseOf(stored.Cause, stored.Spelled);
            var about = stored.About.Length == 0 ? null : stored.About;
            return stored.Ends ? BlockTrouble.Stored(cause, about) : BlockTrouble.Waiting(cause, about);
        }

        /// <summary><paramref name="fault" /> as it is stored, or null for none.</summary>
        public static FaultMemory Stored(SequenceFault fault)
        {
            return fault.Exists
                ? new FaultMemory { Problem = (int)fault.Problem, Step = fault.Step, Which = fault.Which ?? "", Because = Stored(fault.Because) }
                : null;
        }

        /// <summary>The fault <paramref name="stored" /> holds, or none.</summary>
        public static SequenceFault Fault(FaultMemory stored)
        {
            if (stored == null)
                return SequenceFault.None;

            return new SequenceFault(Problem(stored.Problem, stored.Spelled), stored.Step, Trouble(stored.Because),
                stored.Which.Length == 0 ? null : stored.Which);
        }

        /// <summary><paramref name="halt" /> as it is stored, or null where a person switched it off.</summary>
        public static HaltMemory Stored(Halt halt)
        {
            return halt == null
                ? null
                : new HaltMemory
                {
                    Problem = (int)halt.Problem,
                    Sequence = halt.Sequence,
                    Step = halt.Step,
                    Block = halt.Block ?? "",
                    Because = Stored(halt.Because)
                };
        }

        /// <summary>The halt <paramref name="stored" /> holds, or null where a person switched it off.</summary>
        public static Halt Halt(HaltMemory stored)
        {
            return stored == null
                ? null
                : new Halt(Problem(stored.Problem, stored.Spelled), stored.Sequence, stored.Step, Trouble(stored.Because),
                    stored.Block.Length == 0 ? null : stored.Block);
        }
    }
}
