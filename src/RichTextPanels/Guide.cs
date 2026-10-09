namespace RichTextPanels
{
    /// <summary>The guide to writing a panel's text, as markdown-lite: each part shows what to write, then what it gives.</summary>
    public static class Guide
    {
        /// <summary>The guide.</summary>
        public const string Text =
            "Write in the panel's text with **Edit text**. Each part below shows what to write, then what it gives.\n" +
            "\n" +
            "## Bold, italic and code\n" +
            "\n" +
            "`` **bold**  *italic*  ***both***  `code` ``\n" +
            "\n" +
            "**bold**  *italic*  ***both***  `code`\n" +
            "\n" +
            "## Headings\n" +
            "\n" +
            "`# Large`\n" +
            "`## Middle`\n" +
            "`### Small`\n" +
            "\n" +
            "# Large\n" +
            "\n" +
            "## Middle\n" +
            "\n" +
            "### Small\n" +
            "\n" +
            "## Lines and paragraphs\n" +
            "\n" +
            "One line break starts a new line. A blank line between two lines starts a new paragraph.\n" +
            "\n" +
            "## Lists\n" +
            "\n" +
            "`- Fuel`\n" +
            "`- Oxygen`\n" +
            "`1. Close the door`\n" +
            "`2. Open the vent`\n" +
            "\n" +
            "- Fuel\n" +
            "- Oxygen\n" +
            "\n" +
            "1. Close the door\n" +
            "2. Open the vent\n" +
            "\n" +
            "## Quotes\n" +
            "\n" +
            "`> The outer door seals at full pressure.`\n" +
            "\n" +
            "> The outer door seals at full pressure.\n" +
            "\n" +
            "## Rules\n" +
            "\n" +
            "`---`\n" +
            "\n" +
            "---\n" +
            "\n" +
            "## Centered and right\n" +
            "\n" +
            "`::: center`\n" +
            "`Docking bay 2`\n" +
            "`:::`\n" +
            "\n" +
            "::: center\n" +
            "Docking bay 2\n" +
            ":::\n" +
            "\n" +
            "`::: right` places a part on the right the same way.\n" +
            "\n" +
            "## Tables\n" +
            "\n" +
            "`| Tank | Fill |`\n" +
            "`|:--|--:|`\n" +
            "`| Oxygen | 80% |`\n" +
            "`| Hydrogen | 35% |`\n" +
            "\n" +
            "| Tank | Fill |\n" +
            "|:--|--:|\n" +
            "| Oxygen | 80% |\n" +
            "| Hydrogen | 35% |\n" +
            "\n" +
            "The second line says how each column lines up: `:--` left, `:-:` centered, `--:` right.\n" +
            "\n" +
            "A table without a heading starts with a row of empty cells:\n" +
            "\n" +
            "`| | |`\n" +
            "`|:--|--:|`\n" +
            "`| Oxygen | 80% |`\n" +
            "`| Hydrogen | 35% |`\n" +
            "\n" +
            "| | |\n" +
            "|:--|--:|\n" +
            "| Oxygen | 80% |\n" +
            "| Hydrogen | 35% |\n" +
            "\n" +
            "A table is as wide as its cells. A star in a column's dashes, as in `:--*`, gives that column the width the " +
            "screen has to spare, so the table reaches across it; with stars in more than one column, each star is an " +
            "equal share.\n" +
            "\n" +
            "`| Tank | Fill |`\n" +
            "`|:--*|--:|`\n" +
            "`| Oxygen | 80% |`\n" +
            "\n" +
            "| Tank | Fill |\n" +
            "|:--*|--:|\n" +
            "| Oxygen | 80% |\n" +
            "\n" +
            "## Pictures\n" +
            "\n" +
            "`![Arrow](Arrow)`  `![Arrow](Arrow =48)`  `![Arrow](Arrow 90deg)`\n" +
            "\n" +
            "![Arrow](Arrow)  ![Arrow](Arrow =48)  ![Arrow](Arrow 90deg)\n" +
            "\n" +
            "The name is one of the panel's own pictures, as its image list shows them. `=48` makes it 48 wide, " +
            "`=48x24` gives both sides, and `90deg` turns it clockwise.\n" +
            "\n" +
            "## Writing a mark as itself\n" +
            "\n" +
            "`\\*not italic\\*`\n" +
            "\n" +
            "\\*not italic\\*\n" +
            "\n" +
            "A backslash before a mark writes the mark.";
    }
}
