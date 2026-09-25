namespace Sprocket.Host.SpaceEngineers.Ui.Rich
{
    /// <summary>A stretch of words inside a block that are all set apart the same way, or a line break.</summary>
    public sealed class Run
    {
        /// <summary>A line break.</summary>
        public static readonly Run Break = new Run("", RunKind.Break);

        /// <summary>The words <paramref name="text" />, set apart as <paramref name="kind" /> says.</summary>
        public Run(string text, RunKind kind)
        {
            Text = text ?? "";
            Kind = kind;
        }

        /// <summary>The words, empty for a line break.</summary>
        public string Text { get; }

        /// <summary>How the words are set apart from those around them.</summary>
        public RunKind Kind { get; }

        /// <summary>Whether this ends a line rather than carrying words.</summary>
        public bool IsBreak => (Kind & RunKind.Break) != 0;
    }
}
