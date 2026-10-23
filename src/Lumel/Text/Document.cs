using System.Collections.Generic;

namespace Lumel.Text
{
    /// <summary>Text with structure: blocks in order, each holding runs of words, whatever format it was written in.</summary>
    public sealed class Document
    {
        readonly List<Block> _blocks = new List<Block>();

        /// <summary>The blocks, in order.</summary>
        public IList<Block> Blocks => _blocks;

        /// <summary>
        /// This document without its title, a heading of the first level it opens with, which is given out as <paramref name="title" />;
        /// this document itself, and no title, where it opens with none.
        /// </summary>
        public Document Untitled(out Document title)
        {
            if (_blocks.Count == 0 || _blocks[0].Kind != BlockKind.Heading || _blocks[0].Level != 1)
            {
                title = null;
                return this;
            }

            title = new Document();
            title._blocks.Add(_blocks[0]);

            var rest = new Document();
            for (var i = 1; i < _blocks.Count; i++)
                rest._blocks.Add(_blocks[i]);

            return rest;
        }

        /// <summary>A document of one paragraph holding <paramref name="text" /> as it is, each newline a line break.</summary>
        public static Document Plain(string text)
        {
            var document = new Document();
            var paragraph = new Block(BlockKind.Paragraph);
            document.Blocks.Add(paragraph);

            if (string.IsNullOrEmpty(text))
                return document;

            var start = 0;
            for (var i = 0; i <= text.Length; i++)
            {
                if (i < text.Length && text[i] != '\n')
                    continue;

                if (i > start)
                    paragraph.Runs.Add(new Run(text.Substring(start, i - start), RunKind.Plain));

                if (i < text.Length)
                    paragraph.Runs.Add(Run.Break);

                start = i + 1;
            }

            return document;
        }
    }
}
