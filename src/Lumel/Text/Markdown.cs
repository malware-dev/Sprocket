using System;
using System.Collections.Generic;
using System.Text;

namespace Lumel.Text
{
    /// <summary>Converts markdown-lite to a document, and makes any string safe to put into markdown-lite.</summary>
    /// <remarks>
    ///     Markdown-lite is CommonMark's headings (<c>#</c> to <c>######</c>), rules (<c>---</c>, <c>***</c>,
    ///     <c>___</c>, spaced or not), one level of lists (<c>-</c>, <c>*</c> or <c>+</c>, and <c>1.</c> or
    ///     <c>1)</c>), quotes (<c>&gt;</c>, one level), boxes (<c>:::</c>), GitHub's tables, strong and emphasis with <c>*</c> and <c>_</c>, code spans, images
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
    ///     <para>
    ///         A line of three or more <c>:</c> and then names opens a box, and a line of only <c>:</c> closes the
    ///         innermost one; what is between is the box's blocks. The names are attributes, letters, digits,
    ///         <c>-</c> and <c>_</c>. <c>left</c>, <c>center</c> and <c>right</c> set where the box's lines sit, and
    ///         every other name means what the host says it does; a box inside a box adds its own to those around
    ///         it. A box never closed runs to the end, and a closing line with no box open is words. Boxes nest
    ///         <see cref="Deepest" /> deep at most; a fence that would open one deeper is words.
    ///     </para>
    /// </remarks>
    public static class Markdown
    {
        /// <summary>How many boxes deep a document can nest.</summary>
        /// <remarks>
        ///     Reading a box and laying it out each go one call deeper per box, so a document that could nest without limit
        ///     could run the stack out, which takes the game down.
        /// </remarks>
        public const int Deepest = 16;

        /// <summary>The document <paramref name="source" /> describes.</summary>
        public static Document Parse(string source)
        {
            var document = new Document();
            if (string.IsNullOrEmpty(source))
                return document;

            var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            Read(lines, 0, lines.Length, document.Blocks, false, 0, Pairs(lines));
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

        /// <summary>Reads <paramref name="lines" /> from <paramref name="from" /> up to <paramref name="to" /> into <paramref name="blocks" />, <paramref name="boxed" /> boxes deep, their fences paired as <paramref name="closes" /> says; inside a quote, a line starting with <c>&gt;</c> is only words.</summary>
        static void Read(string[] lines, int from, int to, IList<Block> blocks, bool quoted, int boxed, int[] closes)
        {
            var open = new StringBuilder();
            Block current = null;
            Block table = null;

            for (var i = from; i < to; i++)
            {
                var line = lines[i].Trim();

                if (line.Length == 0)
                {
                    Close(blocks, ref current, open);
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

                List<string> names;
                if (boxed < Deepest && IsFence(line, out names) && names.Count > 0)
                {
                    Close(blocks, ref current, open);
                    i = Box(lines, i, to, blocks, quoted, names, boxed, closes) - 1;
                    continue;
                }

                if (!quoted && line[0] == '>')
                {
                    Close(blocks, ref current, open);
                    i = Quote(lines, i, to, blocks, boxed) - 1;
                    continue;
                }

                int level;
                string rest;

                if (IsRule(line))
                {
                    Close(blocks, ref current, open);
                    blocks.Add(new Block(BlockKind.Rule));
                    continue;
                }

                if (IsHeading(line, out level, out rest))
                {
                    Close(blocks, ref current, open);
                    current = new Block(BlockKind.Heading, level);
                    open.Append(rest);
                    Close(blocks, ref current, open);
                    continue;
                }

                if (IsBullet(line, out rest))
                {
                    Close(blocks, ref current, open);
                    current = new Block(BlockKind.Bullet);
                    open.Append(rest);
                    continue;
                }

                if (IsNumbered(line, out level, out rest))
                {
                    Close(blocks, ref current, open);
                    current = new Block(BlockKind.Numbered, level);
                    open.Append(rest);
                    continue;
                }

                List<Column> columns;
                if (current != null && current.Kind == BlockKind.Paragraph && IsDelimiterRow(line, out columns) &&
                    Cells(LastLine(open)).Count == columns.Count)
                {
                    table = Table(blocks, ref current, open, columns);
                    continue;
                }

                if (current == null)
                    current = new Block(BlockKind.Paragraph);
                else
                    open.Append('\n');

                open.Append(line);
            }

            Close(blocks, ref current, open);
        }

        /// <summary>Reads the quote whose lines start at <paramref name="from" />, up to the first line before <paramref name="to" /> that does not start with <c>&gt;</c>, into <paramref name="blocks" />; answers where that line is.</summary>
        /// <remarks>Each line loses its <c>&gt;</c> and one space after it.</remarks>
        static int Quote(string[] lines, int from, int to, IList<Block> blocks, int boxed)
        {
            var end = from;
            while (end < to)
            {
                var line = lines[end].Trim();
                if (line.Length == 0 || line[0] != '>')
                    break;

                end++;
            }

            var inner = new string[end - from];
            for (var i = 0; i < inner.Length; i++)
            {
                var line = lines[from + i].Trim();
                inner[i] = line.Length > 1 && line[1] == ' ' ? line.Substring(2) : line.Substring(1);
            }

            var quote = new Block(BlockKind.Quote);
            Read(inner, 0, inner.Length, quote.Blocks, true, boxed, Pairs(inner));
            blocks.Add(quote);
            return end;
        }

        /// <summary>Reads the box opened at <paramref name="from" /> asking for <paramref name="names" />, up to the fence that closes it or <paramref name="to" />, into <paramref name="blocks" />; answers where reading goes on.</summary>
        /// <remarks>The fence closing it is the one <paramref name="closes" /> pairs it with, so a box inside it closes first.</remarks>
        static int Box(string[] lines, int from, int to, IList<Block> blocks, bool quoted, List<string> names, int boxed, int[] closes)
        {
            var close = closes[from];
            var end = close >= 0 && close < to ? close : to;

            var box = new Block(names);
            Read(lines, from + 1, end, box.Blocks, quoted, boxed + 1, closes);
            blocks.Add(box);
            return end < to ? end + 1 : to;
        }

        /// <summary>For each line of <paramref name="lines" /> that opens a box, the line of the fence that closes it, or -1 where none does; null where no line opens one.</summary>
        /// <remarks>
        ///     One pass, pairing each closing fence with the innermost box still open, so finding where a box ends costs
        ///     nothing however deep boxes nest. A closing fence with no box open pairs with nothing, and is words.
        /// </remarks>
        static int[] Pairs(string[] lines)
        {
            int[] closes = null;
            List<int> open = null;

            for (var i = 0; i < lines.Length; i++)
            {
                var fence = FenceOf(lines[i]);
                if (fence == Fence.Opens)
                {
                    if (closes == null)
                    {
                        closes = new int[lines.Length];
                        for (var j = 0; j < closes.Length; j++)
                            closes[j] = -1;

                        open = new List<int>();
                    }

                    open.Add(i);
                }
                else if (fence == Fence.Closes && open != null && open.Count > 0)
                {
                    closes[open[open.Count - 1]] = i;
                    open.RemoveAt(open.Count - 1);
                }
            }

            return closes;
        }

        /// <summary>What <paramref name="line" /> is as a fence, read in place: as <see cref="IsFence" /> reads it, without making its names.</summary>
        static Fence FenceOf(string line)
        {
            var start = 0;
            var end = line.Length;
            while (start < end && char.IsWhiteSpace(line[start]))
                start++;

            while (end > start && char.IsWhiteSpace(line[end - 1]))
                end--;

            if (end - start < 3 || line[start] != ':' || line[start + 1] != ':' || line[start + 2] != ':')
                return Fence.None;

            var at = start + 3;
            while (at < end && line[at] == ':')
                at++;

            var named = false;
            for (; at < end; at++)
            {
                var c = line[at];
                if (c == ' ' || c == '\t')
                    continue;

                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                    return Fence.None;

                named = true;
            }

            return named ? Fence.Opens : Fence.Closes;
        }

        /// <summary>Whether <paramref name="line" /> is a box's fence: three or more <c>:</c>, then the names it asks for, letters, digits, <c>-</c> and <c>_</c>, or none where it closes a box.</summary>
        static bool IsFence(string line, out List<string> names)
        {
            names = null;
            if (line.Length < 3 || line[0] != ':' || line[1] != ':' || line[2] != ':')
                return false;

            var at = 3;
            while (at < line.Length && line[at] == ':')
                at++;

            var words = line.Substring(at).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (var w = 0; w < words.Length; w++)
            {
                var word = words[w];
                for (var i = 0; i < word.Length; i++)
                {
                    if (!char.IsLetterOrDigit(word[i]) && word[i] != '-' && word[i] != '_')
                        return false;
                }
            }

            names = new List<string>(words);
            return true;
        }

        static void Close(IList<Block> blocks, ref Block current, StringBuilder open)
        {
            if (current == null)
                return;

            Inlines.Parse(open.ToString(), current.Runs);
            blocks.Add(current);
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
        static Block Table(IList<Block> blocks, ref Block current, StringBuilder open, List<Column> columns)
        {
            var heading = LastLine(open);

            if (heading.Length < open.Length)
            {
                open.Length -= heading.Length + 1;
                Close(blocks, ref current, open);
            }
            else
            {
                current = null;
                open.Clear();
            }

            var table = new Block(BlockKind.Table);
            for (var i = 0; i < columns.Count; i++)
                table.Columns.Add(columns[i]);

            blocks.Add(table);

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

        /// <summary>What a line is as a fence.</summary>
        enum Fence
        {
            None,
            Opens,
            Closes
        }
    }
}
