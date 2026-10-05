
namespace Lumel.Text
{
    /// <summary>What a block of a document is.</summary>
    public enum BlockKind
    {
        /// <summary>Running words.</summary>
        Paragraph,

        /// <summary>A heading, at the level the block carries.</summary>
        Heading,

        /// <summary>An item of a list whose items are marked alike.</summary>
        Bullet,

        /// <summary>An item of a list whose items are counted, at the number the block carries.</summary>
        Numbered,

        /// <summary>A line across, dividing what is above from what is below.</summary>
        Rule,

        /// <summary>Rows of cells in columns: its blocks are its rows, and its columns say how each sits.</summary>
        Table,

        /// <summary>One row of a table: its blocks are its cells, one for each column; level 1 for the heading row.</summary>
        Row,

        /// <summary>One cell of a table's row, holding words; level 1 in the heading row.</summary>
        Cell,

        /// <summary>Blocks set apart as quoted: its blocks are what it holds, and its tag, where it has one, names how it looks.</summary>
        Quote
    }
}
