
namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// What is wrong with a sequence, and which line of it is wrong.
    /// </summary>
    /// <remarks>
    /// The problem alone was enough while it only had to be printed. Once a screen has to lead
    /// somebody to the fix it has to say where as well, because "this affects something that isn't
    /// here any more" in front of a nine-step sequence is an accusation without an address.
    /// <para>
    /// One fault, not all of them. A sequence with two broken steps is mended one step at a time,
    /// and the second is still broken when the first is fixed, so it reports itself then. A line
    /// that tried to say both would say neither well.
    /// </para>
    /// </remarks>
    struct SequenceFault
    {
        /// <summary>What <see cref="Step" /> holds when the line at fault is the one that starts it.</summary>
        public const int Trigger = -1;

        public static readonly SequenceFault None = new SequenceFault(SequenceProblem.None, Trigger);

        public SequenceFault(SequenceProblem problem, int step) : this(problem, step, null)
        {
        }

        /// <summary>The same, where the thing at fault said in its own words what is wrong.</summary>
        /// <remarks>
        /// Most problems are the same sentence every time they happen, and that sentence lives in
        /// the wording. A block that cannot finish is not: only it knows why, and the reason is
        /// worth more than the category.
        /// </remarks>
        public SequenceFault(SequenceProblem problem, int step, string because) : this()
        {
            Problem = problem;
            Step = step;
            Because = because;
        }

        public string Because { get; private set; }

        public SequenceProblem Problem { get; private set; }

        /// <summary>
        /// Which step is at fault, or <see cref="Trigger" /> for the line that starts it.
        /// </summary>
        /// <remarks>Says nothing while <see cref="Exists" /> is false.</remarks>
        public int Step { get; private set; }

        /// <summary>Whether there is anything wrong at all.</summary>
        public bool Exists => Problem != SequenceProblem.None;

        /// <summary>
        /// Whether this is worth saying quietly rather than marking as broken.
        /// </summary>
        /// <remarks>
        /// Decided by what it is rather than by whoever is showing it, so every screen agrees about how
        /// loud to be. A warning says its words and washes its row amber; what it does not do is read
        /// as broken, add to what is counted as wrong, or get the sequence parked.
        /// </remarks>
        public bool IsWarning => Problem == SequenceProblem.NothingStarts
            || Problem == SequenceProblem.SwitchedOff
            || Problem == SequenceProblem.Conflicted;

        /// <summary>
        /// Whether this is reason to switch the sequence off.
        /// </summary>
        /// <remarks>
        /// Not the same question as whether it is a fault, which is why it is asked separately.
        /// Switching off somebody's automation is allowed in one case only: the sequence is already
        /// not running and cannot mend itself, so there is nothing to interrupt and nothing to wait
        /// for. A sequence held mid-run by the world - a vent with nowhere to put the air, a room
        /// with no oxygen - fails both halves. It is running, so switching it off is exactly the
        /// half-finished machine parking exists to prevent; and the world can mend it without
        /// anybody editing anything, at which point it carries on by itself.
        /// </remarks>
        public bool Parks => Exists && !IsWarning && Problem != SequenceProblem.Stuck;
    }
}
