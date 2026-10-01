using System.Collections.Generic;

namespace Lumel.Text
{
    /// <summary>Text with structure: blocks in order, each holding runs of words, whatever format it was written in.</summary>
    public sealed class Document
    {
        readonly List<Block> _blocks = new List<Block>();

        /// <summary>The blocks, in order.</summary>
        public IList<Block> Blocks => _blocks;

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
