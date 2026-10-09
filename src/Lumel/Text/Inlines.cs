using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lumel.Text
{
    /// <summary>Reads the words inside one block of markdown-lite into runs: strong, emphasis, code spans, images, links, escapes and line breaks.</summary>
    /// <remarks>
    ///     Strong and emphasis follow CommonMark's delimiter runs: a run of <c>*</c> or <c>_</c> opens or
    ///     closes by what stands either side of it, a closer takes the nearest opener of its own character,
    ///     two on both sides make strong and one makes emphasis, and <c>_</c> inside a word is only a
    ///     character.
    /// </remarks>
    public static class Inlines
    {
        /// <summary>Adds the runs <paramref name="text" /> holds to <paramref name="runs" />.</summary>
        public static void Parse(string text, IList<Run> runs)
        {
            var pieces = Split(text);
            Match(pieces);
            Emit(pieces, runs);
        }

        /// <summary>Whether <paramref name="c" /> is one of the ASCII punctuation marks a backslash escapes.</summary>
        public static bool IsAsciiPunctuation(char c)
        {
            return c >= '!' && c <= '/' || c >= ':' && c <= '@' || c >= '[' && c <= '`' || c >= '{' && c <= '~';
        }

        /// <summary>Cuts the text into words, code spans, line breaks and runs of delimiters.</summary>
        static List<Piece> Split(string text)
        {
            var pieces = new List<Piece>();
            var words = new StringBuilder();
            var i = 0;

            while (i < text.Length)
            {
                var c = text[i];

                if (c == '\\' && i + 1 < text.Length && IsAsciiPunctuation(text[i + 1]))
                {
                    words.Append(text[i + 1]);
                    i += 2;
                    continue;
                }

                if (c == '\n')
                {
                    Flush(pieces, words);
                    pieces.Add(new Piece { Kind = PieceKind.Break });
                    i++;
                    continue;
                }

                if (c == '`')
                {
                    var ticks = Count(text, i, '`');
                    var close = Closing(text, i + ticks, ticks);
                    if (close < 0)
                    {
                        words.Append('`', ticks);
                        i += ticks;
                        continue;
                    }

                    Flush(pieces, words);
                    pieces.Add(new Piece { Kind = PieceKind.Code, Text = Code(text.Substring(i + ticks, close - i - ticks)) });
                    i = close + ticks;
                    continue;
                }

                if (c == '!' && i + 1 < text.Length && text[i + 1] == '[')
                {
                    int end;
                    var image = Image(text, i, out end);
                    if (image != null)
                    {
                        Flush(pieces, words);
                        pieces.Add(image);
                        i = end;
                        continue;
                    }
                }

                if (c == '[')
                {
                    int end;
                    var link = Link(text, i, out end);
                    if (link != null)
                    {
                        Flush(pieces, words);
                        pieces.Add(link);
                        i = end;
                        continue;
                    }
                }

                if (c == '*' || c == '_')
                {
                    var count = Count(text, i, c);
                    var before = i > 0 ? text[i - 1] : ' ';
                    var after = i + count < text.Length ? text[i + count] : ' ';
                    var left = !IsSpace(after) && (!IsPunctuation(after) || IsSpace(before) || IsPunctuation(before));
                    var right = !IsSpace(before) && (!IsPunctuation(before) || IsSpace(after) || IsPunctuation(after));

                    Flush(pieces, words);
                    pieces.Add(new Piece
                    {
                        Kind = PieceKind.Delimiter,
                        Mark = c,
                        Left = count,
                        Length = count,
                        Opens = c == '*' ? left : left && (!right || IsPunctuation(before)),
                        Closes = c == '*' ? right : right && (!left || IsPunctuation(after))
                    });

                    i += count;
                    continue;
                }

                words.Append(c);
                i++;
            }

            Flush(pieces, words);
            return pieces;
        }

        /// <summary>Pairs closers with openers, and marks where each pair's span starts and ends.</summary>
        /// <remarks>
        ///     CommonMark's own procedure, and linear in the number of delimiters: openers wait on a stack; a
        ///     match drops every opener between the pair from it, since those can no longer match anything;
        ///     and a search that finds nothing records how far down it looked, per kind of closer, so the
        ///     next closer of that kind starts above it. A span is marked at its two ends rather than piece
        ///     by piece, and <see cref="Emit" /> counts the depth as it passes.
        /// </remarks>
        static void Match(List<Piece> pieces)
        {
            var stack = new List<int>();
            var floors = new int[12];
            for (var i = 0; i < floors.Length; i++)
                floors[i] = -1;

            for (var closer = 0; closer < pieces.Count; closer++)
            {
                var c = pieces[closer];
                if (c.Kind != PieceKind.Delimiter)
                    continue;

                if (!c.Closes)
                {
                    if (c.Opens)
                        stack.Add(closer);

                    continue;
                }

                var kind = (c.Mark == '*' ? 0 : 6) + c.Length % 3 * 2 + (c.Opens ? 1 : 0);

                while (c.Left > 0)
                {
                    var at = Opener(pieces, stack, c, floors[kind]);
                    if (at < 0)
                    {
                        floors[kind] = closer;
                        break;
                    }

                    var opener = stack[at];
                    var o = pieces[opener];
                    var strong = o.Left >= 2 && c.Left >= 2;

                    if (strong)
                    {
                        pieces[opener + 1].StrongDelta++;
                        c.StrongDelta--;
                    }
                    else
                    {
                        pieces[opener + 1].EmphasisDelta++;
                        c.EmphasisDelta--;
                    }

                    o.Left -= strong ? 2 : 1;
                    c.Left -= strong ? 2 : 1;

                    stack.RemoveRange(at + 1, stack.Count - at - 1);
                    if (o.Left == 0)
                        stack.RemoveAt(at);
                }

                if (c.Left > 0 && c.Opens)
                    stack.Add(closer);
            }
        }

        /// <summary>Where on <paramref name="stack" /> the nearest opener <paramref name="closer" /> can close is, looking no lower than <paramref name="floor" />, or -1.</summary>
        /// <remarks>
        ///     CommonMark's rule of three: where either side can both open and close, a pair whose lengths add
        ///     to a multiple of three does not match unless both lengths are.
        /// </remarks>
        static int Opener(List<Piece> pieces, List<int> stack, Piece closer, int floor)
        {
            for (var i = stack.Count - 1; i >= 0 && stack[i] > floor; i--)
            {
                var o = pieces[stack[i]];
                if (o.Mark != closer.Mark || o.Left == 0)
                    continue;

                if ((o.Closes || closer.Opens) && (o.Length + closer.Length) % 3 == 0
                                               && (o.Length % 3 != 0 || closer.Length % 3 != 0))
                    continue;

                return i;
            }

            return -1;
        }

        /// <summary>Turns the pieces into runs, joining neighbours that are set apart alike.</summary>
        static void Emit(List<Piece> pieces, IList<Run> runs)
        {
            var words = new StringBuilder();
            var kind = RunKind.Plain;
            var strong = 0;
            var emphasis = 0;

            for (var i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                strong += piece.StrongDelta;
                emphasis += piece.EmphasisDelta;

                if (piece.Kind == PieceKind.Break)
                {
                    Add(runs, words, kind);
                    runs.Add(Run.Break);
                    continue;
                }

                if (piece.Kind == PieceKind.Image)
                {
                    Add(runs, words, kind);
                    var stressed = (strong > 0 ? RunKind.Strong : RunKind.Plain) | (emphasis > 0 ? RunKind.Emphasis : RunKind.Plain);
                    runs.Add(new ImageRun(((ImagePiece)piece).Picture, piece.Text, stressed));
                    continue;
                }

                if (piece.Kind == PieceKind.Link)
                {
                    Add(runs, words, kind);
                    var stressed = (strong > 0 ? RunKind.Strong : RunKind.Plain) | (emphasis > 0 ? RunKind.Emphasis : RunKind.Plain);
                    runs.Add(new LinkRun(((LinkPiece)piece).To, piece.Text, stressed));
                    continue;
                }

                var text = piece.Kind == PieceKind.Delimiter ? new string(piece.Mark, piece.Left) : piece.Text;
                if (text.Length == 0)
                    continue;

                var its = (strong > 0 ? RunKind.Strong : RunKind.Plain)
                          | (emphasis > 0 ? RunKind.Emphasis : RunKind.Plain)
                          | (piece.Kind == PieceKind.Code ? RunKind.Code : RunKind.Plain);

                if (its != kind)
                {
                    Add(runs, words, kind);
                    kind = its;
                }

                words.Append(text);
            }

            Add(runs, words, kind);
        }

        static void Add(IList<Run> runs, StringBuilder words, RunKind kind)
        {
            if (words.Length == 0)
                return;

            runs.Add(new Run(words.ToString(), kind));
            words.Clear();
        }

        static void Flush(List<Piece> pieces, StringBuilder words)
        {
            if (words.Length == 0)
                return;

            pieces.Add(new Piece { Kind = PieceKind.Words, Text = words.ToString() });
            words.Clear();
        }

        /// <summary>The image written at <paramref name="at" /> as <c>![words](locator)</c>, with a size <c>=size</c> and a turn <c>90deg</c> after the locator if wanted, and where it ends; null where none is written there.</summary>
        /// <remarks>The words in the brackets are only words, read for nothing, as an image's description. The size and the turn may come in either order, each once.</remarks>
        static ImagePiece Image(string text, int at, out int end)
        {
            end = at;

            var close = Bracket(text, at + 2, ']');
            if (close < 0 || close + 1 >= text.Length || text[close + 1] != '(')
                return null;

            var shut = Bracket(text, close + 2, ')');
            if (shut < 0)
                return null;

            var inside = text.Substring(close + 2, shut - close - 2).Trim();
            var space = inside.IndexOf(' ');
            var locator = space < 0 ? inside : inside.Substring(0, space);
            if (locator.Length == 0)
                return null;

            var width = float.NaN;
            var height = float.NaN;
            var turn = 0f;
            var sized = false;
            var turned = false;
            var i = space;
            while (i >= 0 && i < inside.Length)
            {
                while (i < inside.Length && IsSpace(inside[i]))
                    i++;

                if (i >= inside.Length)
                    break;

                var next = i;
                while (next < inside.Length && !IsSpace(inside[next]))
                    next++;

                var word = inside.Substring(i, next - i);
                if (!sized && IsSize(word, out width, out height))
                    sized = true;
                else if (!turned && IsTurn(word, out turn))
                    turned = true;
                else
                    return null;

                i = next;
            }

            end = shut + 1;
            return new ImagePiece
            {
                Kind = PieceKind.Image,
                Text = Unescaped(text, at + 2, close),
                Picture = new Picture(locator, width, height, turn)
            };
        }

        /// <summary>The link written at <paramref name="at" /> as <c>[words](target)</c>, and where it ends; null where none is written there.</summary>
        /// <remarks>A target holds no spaces, as a link destination in CommonMark does not; the words are only words.</remarks>
        static LinkPiece Link(string text, int at, out int end)
        {
            end = at;

            var close = Bracket(text, at + 1, ']');
            if (close < 0 || close + 1 >= text.Length || text[close + 1] != '(')
                return null;

            var shut = Bracket(text, close + 2, ')');
            if (shut < 0)
                return null;

            var target = text.Substring(close + 2, shut - close - 2).Trim();
            if (target.Length == 0)
                return null;

            for (var i = 0; i < target.Length; i++)
            {
                if (IsSpace(target[i]))
                    return null;
            }

            end = shut + 1;
            return new LinkPiece
            {
                Kind = PieceKind.Link,
                Text = Unescaped(text, at + 1, close),
                To = new Link(target)
            };
        }

        /// <summary>The text from <paramref name="from" /> up to <paramref name="to" />, each escaped punctuation mark only the mark.</summary>
        static string Unescaped(string text, int from, int to)
        {
            if (text.IndexOf('\\', from, to - from) < 0)
                return text.Substring(from, to - from);

            var words = new StringBuilder(to - from);
            for (var i = from; i < to; i++)
            {
                if (text[i] == '\\' && i + 1 < to && IsAsciiPunctuation(text[i + 1]))
                    i++;

                words.Append(text[i]);
            }

            return words.ToString();
        }

        /// <summary>Whether <paramref name="text" /> is an image's size: <c>=WxH</c>, or <c>=W</c>, <c>=Wx</c> or <c>=xH</c> for one side, each a number above zero.</summary>
        static bool IsSize(string text, out float width, out float height)
        {
            width = float.NaN;
            height = float.NaN;

            if (text.Length < 2 || text[0] != '=')
                return false;

            var by = text.IndexOf('x');
            var across = by < 0 ? text.Substring(1) : text.Substring(1, by - 1);
            var down = by < 0 ? "" : text.Substring(by + 1);

            return (across.Length > 0 || down.Length > 0)
                   && (across.Length == 0 || IsNumber(across, out width))
                   && (down.Length == 0 || IsNumber(down, out height));
        }

        /// <summary>Whether <paramref name="text" /> is an image's turn: degrees clockwise followed by <c>deg</c>, as <c>90deg</c> or <c>-12.5deg</c>.</summary>
        static bool IsTurn(string text, out float degrees)
        {
            degrees = 0f;
            if (text.Length < 4 || !text.EndsWith("deg", StringComparison.Ordinal))
                return false;

            float read;
            if (!float.TryParse(text.Substring(0, text.Length - 3), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out read) || float.IsInfinity(read))
                return false;

            degrees = read;
            return true;
        }

        static bool IsNumber(string text, out float number)
        {
            if (float.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number) && number > 0f)
                return true;

            number = float.NaN;
            return false;
        }

        /// <summary>Where <paramref name="closer" /> next stands from <paramref name="from" />, or -1 where another bracket or a line's end comes first; an escaped character is stepped over.</summary>
        /// <remarks>
        ///     Stopping at the next bracket is what keeps reading linear: an image or a link left unclosed is given
        ///     up at the next one rather than searched for to the end of the text, which a run of them would do for each.
        /// </remarks>
        static int Bracket(string text, int from, char closer)
        {
            for (var i = from; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '\\' && i + 1 < text.Length && IsAsciiPunctuation(text[i + 1]))
                {
                    i++;
                    continue;
                }

                if (c == closer)
                    return i;

                if (c == '[' || c == ']' || c == '(' || c == ')' || c == '\n')
                    return -1;
            }

            return -1;
        }

        static int Count(string text, int from, char c)
        {
            var count = 0;
            while (from + count < text.Length && text[from + count] == c)
                count++;

            return count;
        }

        /// <summary>Where a run of exactly <paramref name="ticks" /> backticks starts at or after <paramref name="from" />, or -1.</summary>
        static int Closing(string text, int from, int ticks)
        {
            var i = from;
            while (i < text.Length)
            {
                if (text[i] != '`')
                {
                    i++;
                    continue;
                }

                var run = Count(text, i, '`');
                if (run == ticks)
                    return i;

                i += run;
            }

            return -1;
        }

        /// <summary>A code span's content: line breaks as spaces, and one space stripped from each end where both have one.</summary>
        static string Code(string content)
        {
            content = content.Replace('\n', ' ');

            if (content.Length >= 2 && content[0] == ' ' && content[content.Length - 1] == ' ' && content.Trim().Length > 0)
                content = content.Substring(1, content.Length - 2);

            return content;
        }

        static bool IsSpace(char c)
        {
            return char.IsWhiteSpace(c);
        }

        static bool IsPunctuation(char c)
        {
            return char.IsPunctuation(c) || char.IsSymbol(c);
        }

        enum PieceKind
        {
            Words,
            Code,
            Break,
            Delimiter,
            Image,
            Link
        }

        /// <summary>One cut of the text, and where the spans it sits in begin and end.</summary>
        class Piece
        {
            public PieceKind Kind;
            public string Text = "";
            public char Mark;

            /// <summary>How many delimiters are left unmatched.</summary>
            public int Left;

            /// <summary>How long the run was to begin with, which the rule of three reads.</summary>
            public int Length;

            public bool Opens;
            public bool Closes;

            /// <summary>How much deeper in strong spans this piece is than the one before it.</summary>
            public int StrongDelta;

            /// <summary>How much deeper in emphasis spans this piece is than the one before it.</summary>
            public int EmphasisDelta;
        }

        /// <summary>An image, whose words are its description.</summary>
        sealed class ImagePiece : Piece
        {
            /// <summary>What the image shows.</summary>
            public Picture Picture;
        }

        /// <summary>A link, whose words are what it says.</summary>
        sealed class LinkPiece : Piece
        {
            /// <summary>Where the link leads.</summary>
            public Link To;
        }
    }
}
