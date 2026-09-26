using System.Text;

namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>Converts markdown-lite to a document, and makes any string safe to put into markdown-lite.</summary>
    /// <remarks>
    ///     Markdown-lite is CommonMark's headings (<c>#</c> to <c>######</c>), rules (<c>---</c>, <c>***</c>,
    ///     <c>___</c>, spaced or not), one level of lists (<c>-</c>, <c>*</c> or <c>+</c>, and <c>1.</c> or
    ///     <c>1)</c>), strong and emphasis with <c>*</c> and <c>_</c>, code spans and backslash escapes. A
    ///     single newline is a line break and a blank line ends a block. Anything else reads as the text it is.
    /// </remarks>
    public static class Markdown
    {
        /// <summary>The document <paramref name="source" /> describes.</summary>
        public static Document Parse(string source)
        {
            var document = new Document();
            if (string.IsNullOrEmpty(source))
                return document;

            var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var open = new StringBuilder();
            Block current = null;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (line.Length == 0)
                {
                    Close(document, ref current, open);
                    continue;
                }

                int level;
                string rest;

                if (IsRule(line))
                {
                    Close(document, ref current, open);
                    document.Blocks.Add(new Block(BlockKind.Rule));
                    continue;
                }

                if (IsHeading(line, out level, out rest))
                {
                    Close(document, ref current, open);
                    current = new Block(BlockKind.Heading, level);
                    open.Append(rest);
                    Close(document, ref current, open);
                    continue;
                }

                if (IsBullet(line, out rest))
                {
                    Close(document, ref current, open);
                    current = new Block(BlockKind.Bullet);
                    open.Append(rest);
                    continue;
                }

                if (IsNumbered(line, out level, out rest))
                {
                    Close(document, ref current, open);
                    current = new Block(BlockKind.Numbered, level);
                    open.Append(rest);
                    continue;
                }

                if (current == null)
                    current = new Block(BlockKind.Paragraph);
                else
                    open.Append('\n');

                open.Append(line);
            }

            Close(document, ref current, open);
            return document;
        }

        /// <summary><paramref name="text" /> with every ASCII punctuation mark escaped, so it reads as exactly what it says.</summary>
        /// <remarks>For putting a name somebody typed into markdown-lite.</remarks>
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var escaped = new StringBuilder(text.Length + 8);
            for (var i = 0; i < text.Length; i++)
            {
                if (Inlines.IsAsciiPunctuation(text[i]))
                    escaped.Append('\\');

                escaped.Append(text[i]);
            }

            return escaped.ToString();
        }

        static void Close(Document document, ref Block current, StringBuilder open)
        {
            if (current == null)
                return;

            Inlines.Parse(open.ToString(), current.Runs);
            document.Blocks.Add(current);
            current = null;
            open.Clear();
        }

        /// <summary>Three or more of one of <c>-</c>, <c>*</c> or <c>_</c>, and nothing else but spaces.</summary>
        static bool IsRule(string line)
        {
            var mark = line[0];
            if (mark != '-' && mark != '*' && mark != '_')
                return false;

            var count = 0;
            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == mark)
                    count++;
                else if (line[i] != ' ' && line[i] != '\t')
                    return false;
            }

            return count >= 3;
        }

        /// <summary>One to six <c>#</c> and then a space or nothing; a closing run of <c>#</c> after a space is dropped.</summary>
        static bool IsHeading(string line, out int level, out string rest)
        {
            level = 0;
            rest = null;

            while (level < line.Length && line[level] == '#')
                level++;

            if (level == 0 || level > 6 || level < line.Length && line[level] != ' ' && line[level] != '\t')
                return false;

            rest = line.Substring(level).Trim();

            var end = rest.Length;
            while (end > 0 && rest[end - 1] == '#')
                end--;

            if (end == 0)
                rest = "";
            else if (end < rest.Length && (rest[end - 1] == ' ' || rest[end - 1] == '\t'))
                rest = rest.Substring(0, end).TrimEnd();

            return true;
        }

        /// <summary><c>-</c>, <c>*</c> or <c>+</c>, then a space.</summary>
        static bool IsBullet(string line, out string rest)
        {
            rest = null;

            if (line.Length < 2 || line[0] != '-' && line[0] != '*' && line[0] != '+' || line[1] != ' ' && line[1] != '\t')
                return false;

            rest = line.Substring(2).Trim();
            return true;
        }

        /// <summary>One to nine digits, then <c>.</c> or <c>)</c>, then a space.</summary>
        static bool IsNumbered(string line, out int number, out string rest)
        {
            number = 0;
            rest = null;

            var digits = 0;
            while (digits < line.Length && digits < 10 && line[digits] >= '0' && line[digits] <= '9')
                digits++;

            if (digits == 0 || digits > 9 || digits + 1 >= line.Length)
                return false;

            var mark = line[digits];
            var after = line[digits + 1];
            if (mark != '.' && mark != ')' || after != ' ' && after != '\t')
                return false;

            number = int.Parse(line.Substring(0, digits));
            rest = line.Substring(digits + 2).Trim();
            return true;
        }
    }
}
