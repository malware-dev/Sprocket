using System.Collections.Generic;
using System.Text;

namespace Lumel.Text
{
    /// <summary>Converts markdown-lite to a document, and makes any string safe to put into markdown-lite.</summary>
    /// <remarks>
    ///     Markdown-lite is CommonMark's headings (<c>#</c> to <c>######</c>), rules (<c>---</c>, <c>***</c>,
    ///     <c>___</c>, spaced or not), one level of lists (<c>-</c>, <c>*</c> or <c>+</c>, and <c>1.</c> or
    ///     <c>1)</c>), GitHub's tables, strong and emphasis with <c>*</c> and <c>_</c>, code spans, images
    ///     (<c>![description](name)</c>), links (<c>[words](target)</c>) and backslash escapes. A single newline
    ///     is a line break and a blank line ends a block. Anything else reads as the text it is.
    ///     <para>
    ///         A table's delimiter row may give a column a star for each share it takes of the width the table
    ///         has to spare; a table with no star is as wide as its cells. A heading row of empty cells is no
    ///         heading at all, which is how a table without one is written.
    ///     </para>
    ///     <para>
    ///         An image is as tall as its line unless <c>=WxH</c>, <c>=W</c> or <c>=xH</c> after its name says
    ///         otherwise, a side not given following the picture's shape. A link's target is a string only the
    ///         host reads, and holds no spaces.
    ///     </para>
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
            Block table = null;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (line.Length == 0)
                {
                    Close(document, ref current, open);
                    table = null;
                    continue;
                }

                if (table != null)
                {
                    if (line.IndexOf('|') >= 0)
                    {
                        Row(table, line, 0);
                        continue;
                    }

                    table = null;
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

                List<Column> columns;
                if (current != null && current.Kind == BlockKind.Paragraph && IsDelimiterRow(line, out columns) &&
                    Cells(LastLine(open)).Count == columns.Count)
                {
                    table = Table(document, ref current, open, columns);
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

        /// <summary>The last line of the paragraph being read.</summary>
        static string LastLine(StringBuilder open)
        {
            var text = open.ToString();
            return text.Substring(text.LastIndexOf('\n') + 1);
        }

        /// <summary>A table of <paramref name="columns" />, its heading row the last line of the paragraph being read, and the lines before that left a paragraph.</summary>
        /// <remarks>As GitHub's own reader does, so a table can start straight after the words that introduce it.</remarks>
        static Block Table(Document document, ref Block current, StringBuilder open, List<Column> columns)
        {
            var heading = LastLine(open);

            if (heading.Length < open.Length)
            {
                open.Length -= heading.Length + 1;
                Close(document, ref current, open);
            }
            else
            {
                current = null;
                open.Clear();
            }

            var table = new Block(BlockKind.Table);
            for (var i = 0; i < columns.Count; i++)
                table.Columns.Add(columns[i]);

            document.Blocks.Add(table);

            var cells = Cells(heading);
            for (var i = 0; i < cells.Count; i++)
            {
                if (cells[i].Trim().Length > 0)
                {
                    Row(table, heading, 1);
                    break;
                }
            }

            return table;
        }

        /// <summary>Adds the row written as <paramref name="line" /> to <paramref name="table" />, with as many cells as it has columns: missing ones empty, extra ones dropped.</summary>
        static void Row(Block table, string line, int level)
        {
            var row = new Block(BlockKind.Row, level);
            var cells = Cells(line);

            for (var i = 0; i < table.Columns.Count; i++)
            {
                var cell = new Block(BlockKind.Cell, level);
                Inlines.Parse(i < cells.Count ? cells[i].Trim() : "", cell.Runs);
                row.Blocks.Add(cell);
            }

            table.Blocks.Add(row);
        }

        /// <summary>The cells of a table row: the text between its pipes, with a pipe at either end belonging to neither, and an escaped pipe kept in its cell.</summary>
        static List<string> Cells(string line)
        {
            var cells = new List<string>();
            var from = line.Length > 0 && line[0] == '|' ? 1 : 0;
            var cell = new StringBuilder();

            for (var i = from; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '\\' && i + 1 < line.Length && line[i + 1] == '|')
                {
                    cell.Append("\\|");
                    i++;
                    continue;
                }

                if (c == '|')
                {
                    cells.Add(cell.ToString());
                    cell.Clear();
                    continue;
                }

                cell.Append(c);
            }

            if (cell.Length > 0 || line.Length == 0 || line[line.Length - 1] != '|')
                cells.Add(cell.ToString());

            return cells;
        }

        /// <summary>Whether <paramref name="line" /> is a table's delimiter row: in each cell dashes, a colon at either end for where the column's cells sit, and a star for each share of the spare width the column takes.</summary>
        static bool IsDelimiterRow(string line, out List<Column> columns)
        {
            columns = null;

            // Asked of every line under a paragraph, so the first character turns nearly all of them away
            // before the line is searched.
            var first = line[0];
            if (first != '|' && first != '-' && first != ':' && first != '*' || line.IndexOf('|') < 0 || line.IndexOf('-') < 0)
                return false;

            var cells = Cells(line);
            var found = new List<Column>(cells.Count);

            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i].Trim();
                var shares = 0;
                var marks = new StringBuilder(cell.Length);
                for (var j = 0; j < cell.Length; j++)
                {
                    if (cell[j] == '*')
                        shares++;
                    else
                        marks.Append(cell[j]);
                }

                var bare = marks.ToString();
                var left = bare.Length > 0 && bare[0] == ':';
                var right = bare.Length > 1 && bare[bare.Length - 1] == ':';
                var dashes = bare.Substring(left ? 1 : 0, bare.Length - (left ? 1 : 0) - (right ? 1 : 0));
                if (dashes.Length == 0 || dashes.Trim('-').Length != 0)
                    return false;

                found.Add(new Column(left && right ? Alignment.Center : right ? Alignment.Right : Alignment.Left, shares));
            }

            columns = found;
            return true;
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
