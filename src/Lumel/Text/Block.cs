using System.Collections.Generic;

namespace Lumel.Text
{
    /// <summary>One block of a document: a paragraph, a heading, a list item, a rule, a table, a quote or a box, and the runs of words in it.</summary>
    /// <remarks>A table's blocks are its rows, a row's blocks are its cells, and a quote's or a box's blocks are what it holds; anything else holds runs and no blocks.</remarks>
    public sealed class Block
    {
        static readonly Block[] NoBlocks = new Block[0];
        static readonly Column[] NoColumns = new Column[0];
        static readonly string[] NoNames = new string[0];

        readonly List<Run> _runs = new List<Run>();
        readonly List<Block> _blocks;
        readonly List<Column> _columns;
        readonly List<string> _names;

        /// <summary>An empty block of <paramref name="kind" />; <paramref name="level" /> is a heading's level, a numbered item's number, or 1 for a table's heading row and its cells.</summary>
        /// <remarks>Only a table, a row, a quote and a box hold blocks, and only a table columns; anything else shares one empty list of each, which cannot be added to.</remarks>
        public Block(BlockKind kind, int level = 0)
        {
            Kind = kind;
            Level = level;

            if (kind == BlockKind.Table)
            {
                _blocks = new List<Block>();
                _columns = new List<Column>();
            }
            else if (kind == BlockKind.Row || kind == BlockKind.Quote || kind == BlockKind.Box)
                _blocks = new List<Block>();
        }

        /// <summary>An empty box asking for the attributes <paramref name="names" />.</summary>
        public Block(IList<string> names)
            : this(BlockKind.Box)
        {
            _names = new List<string>(names);
        }

        /// <summary>What the block is.</summary>
        public BlockKind Kind { get; }

        /// <summary>A heading's level, from 1, a numbered item's number, or 1 for a table's heading row and its cells; zero for anything else.</summary>
        public int Level { get; }

        /// <summary>The attributes a box asks for, in the order its fence names them; none for anything else.</summary>
        public IList<string> Names => _names ?? (IList<string>)NoNames;

        /// <summary>The words in the block, in order.</summary>
        public IList<Run> Runs => _runs;

        /// <summary>A table's rows, a row's cells, or what a quote or a box holds, in order.</summary>
        public IList<Block> Blocks => _blocks ?? (IList<Block>)NoBlocks;

        /// <summary>How each of a table's columns sits, in order.</summary>
        public IList<Column> Columns => _columns ?? (IList<Column>)NoColumns;
    }
}
