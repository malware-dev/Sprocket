namespace Lumel.Text
{
    /// <summary>A stretch of words inside a block that are all set apart the same way, a slot whose words are given later, or a line break.</summary>
    /// <remarks>An image is an <see cref="ImageRun" />, and a link's words are a <see cref="LinkRun" />.</remarks>
    public class Run
    {
        /// <summary>The space a line never breaks at.</summary>
        public const char NoBreak = ' ';

        /// <summary>A line break.</summary>
        public static readonly Run Break = new Run("", RunKind.Break);

        /// <summary>The words <paramref name="text" />, set apart as <paramref name="kind" /> says.</summary>
        public Run(string text, RunKind kind)
        {
            Text = text ?? "";
            Kind = kind;
            HoldsNoBreak = Text.IndexOf(NoBreak) >= 0;
        }

        /// <summary>A slot for the argument named <paramref name="slot" />, whose words are set apart as <paramref name="kind" /> says.</summary>
        protected Run(RunKind kind, string slot)
            : this("", kind)
        {
            Slot = slot;
        }

        /// <summary>The words, empty for a line break or a slot.</summary>
        public string Text { get; }

        /// <summary>How the words are set apart from those around them.</summary>
        public RunKind Kind { get; }

        /// <summary>The name of the argument whose words stand here, or null for a run that carries its own.</summary>
        public string Slot { get; }

        /// <summary>Whether its words hold a no-break space, known once so laying it out need not look for one.</summary>
        public bool HoldsNoBreak { get; }

        /// <summary>Whether this ends a line rather than carrying words.</summary>
        public bool IsBreak => (Kind & RunKind.Break) != 0;

        /// <summary>A slot for the argument named <paramref name="name" />, whose words are set apart as <paramref name="kind" /> says.</summary>
        public static Run Slotted(string name, RunKind kind)
        {
            return new Run(kind, name);
        }
    }
}
