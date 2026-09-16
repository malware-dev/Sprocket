namespace Reactor.Host.SpaceEngineers.Core.Editing
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

        public SequenceFault(SequenceProblem problem, int step) : this()
        {
            Problem = problem;
            Step = step;
        }

        public SequenceProblem Problem { get; private set; }

        /// <summary>
        /// Which step is at fault, or <see cref="Trigger" /> for the line that starts it.
        /// </summary>
        /// <remarks>Says nothing while <see cref="Exists" /> is false.</remarks>
        public int Step { get; private set; }

        /// <summary>Whether there is anything wrong at all.</summary>
        public bool Exists => Problem != SequenceProblem.None;
    }
}
