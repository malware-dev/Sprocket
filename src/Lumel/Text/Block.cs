using System.Collections.Generic;

namespace Lumel.Text
{
    /// <summary>One block of a document: a paragraph, a heading, a list item or a rule, and the runs of words in it.</summary>
    public sealed class Block
    {
        readonly List<Run> _runs = new List<Run>();

        /// <summary>An empty block of <paramref name="kind" />; <paramref name="level" /> is a heading's level or a numbered item's number.</summary>
        public Block(BlockKind kind, int level = 0)
        {
            Kind = kind;
            Level = level;
        }

        /// <summary>What the block is.</summary>
        public BlockKind Kind { get; }

        /// <summary>A heading's level, from 1, or a numbered item's number; zero for anything else.</summary>
        public int Level { get; }

        /// <summary>The words in the block, in order.</summary>
        public IList<Run> Runs => _runs;
    }
}
