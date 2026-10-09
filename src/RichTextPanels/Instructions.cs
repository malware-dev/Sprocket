namespace RichTextPanels
{
    /// <summary>What an empty panel says about getting text onto it, at length and in one line.</summary>
    public static class Instructions
    {
        /// <summary>The instructions, for a screen with room for them.</summary>
        public const string Full =
            "# Rich Text\n" +
            "\n" +
            "This panel shows its own text as rich text. It has none yet.\n" +
            "\n" +
            "Press **Edit text** in the panel's terminal, under the script list, and write. Pasting works. " +
            "**Fit to screen**, **Font size**, **Vertical alignment** and **Padding** are there too.\n" +
            "\n" +
            "Write `**bold**`, `*italic*`, `# Heading` and `- list`, and wrap a part in `::: center` and `:::` to center it.";

        /// <summary>The instructions in one line, for a screen too small for them.</summary>
        public const string Brief = "**Rich Text:** no text yet. Press **Edit text** in the terminal.";
    }
}
