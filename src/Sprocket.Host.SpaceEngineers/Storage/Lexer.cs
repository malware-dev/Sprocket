using System.Globalization;
using System.Text;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// Turns script text into tokens, one at a time.
    /// </summary>
    /// <remarks>
    /// Persisted: a quote is escaped by doubling it, a backslash means nothing, and every number is read in the invariant
    /// culture.
    /// </remarks>
    public sealed class Lexer
    {
        TextPtr _ptr;

        /// <summary>A lexer at the start of <paramref name="text" />.</summary>
        public Lexer(string text)
        {
            _ptr = new TextPtr(text ?? "");
        }

        /// <summary>The whole script, so trouble can be placed on a line after the fact.</summary>
        public string Script => _ptr.Content;

        /// <summary>The next token, past any spaces before it.</summary>
        public Token Next()
        {
            _ptr = _ptr.SkipWhitespace();
            if (_ptr.IsOutOfBounds())
                return new Token(TokenKind.End, _ptr.Index);

            if (_ptr.IsNewLine())
            {
                var at = _ptr.Index;
                _ptr = _ptr.FindEndOfLine(true);
                return new Token(TokenKind.Newline, at);
            }

            var c = _ptr.Char;
            switch (c)
            {
                case '{': return One(TokenKind.BlockOpen);
                case '}': return One(TokenKind.BlockClose);
                case '[': return One(TokenKind.ListOpen);
                case ']': return One(TokenKind.ListClose);
                case '(': return One(TokenKind.VectorOpen);
                case ')': return One(TokenKind.VectorClose);
                case ',': return One(TokenKind.Comma);
                case '.': return One(TokenKind.Dot);
                case '=': return Equality();
                case '!': return Different();
                case '<': return Ordered(Keyword.Below, Keyword.AtMost);
                case '>': return Ordered(Keyword.Above, Keyword.AtLeast);
                case '#': return Hash();
                case '/': return Slashes();
                case '"': return Quoted('"', TokenKind.Text);
                case '\'': return Quoted('\'', TokenKind.Identifier);
            }

            if (c == '-' || Digit(c))
                return Number();

            if (Letter(c))
                return Identifier();

            return Bad(_ptr++, $"unexpected '{c}'.");
        }

        static bool Letter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        static bool Digit(char c)
        {
            return c >= '0' && c <= '9';
        }

        static int Hex(char c)
        {
            if (Digit(c))
                return c - '0';
            if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;
            if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;

            return -1;
        }

        Token One(TokenKind kind)
        {
            var at = _ptr.Index;
            _ptr++;
            return new Token(kind, at);
        }

        /// <summary><c>==</c>, or <c>=</c> where it stands alone.</summary>
        Token Equality()
        {
            var start = _ptr;
            _ptr++;
            if (_ptr.IsOutOfBounds() || _ptr.Char != '=')
                return new Token(TokenKind.Equals, start.Index);

            _ptr++;
            return new Token(TokenKind.Comparison, start.Index, keyword: Keyword.EqualTo);
        }

        /// <summary><c>!=</c>, or trouble where <c>!</c> stands alone.</summary>
        Token Different()
        {
            var start = _ptr;
            _ptr++;
            if (_ptr.IsOutOfBounds() || _ptr.Char != '=')
                return Bad(start, "unexpected '!'.");

            _ptr++;
            return new Token(TokenKind.Comparison, start.Index, keyword: Keyword.NotEqualTo);
        }

        /// <summary><paramref name="strict" />, or <paramref name="inclusive" /> where <c>=</c> follows it.</summary>
        Token Ordered(Keyword strict, Keyword inclusive)
        {
            var start = _ptr;
            _ptr++;
            if (_ptr.IsOutOfBounds() || _ptr.Char != '=')
                return new Token(TokenKind.Comparison, start.Index, keyword: strict);

            _ptr++;
            return new Token(TokenKind.Comparison, start.Index, keyword: inclusive);
        }

        /// <summary><c>//</c>, or trouble where a single slash stands alone.</summary>
        Token Slashes()
        {
            var start = _ptr;
            _ptr++;
            if (_ptr.IsOutOfBounds() || _ptr.Char != '/')
                return Bad(start, "unexpected '/'.");

            _ptr++;
            return new Token(TokenKind.Off, start.Index);
        }

        static Token Bad(TextPtr at, string what)
        {
            return new Token(TokenKind.Bad, at.Index, new StringSegment(what));
        }

        StringSegment Since(TextPtr from)
        {
            return new StringSegment(_ptr.Content, from.Index, _ptr.Index - from.Index);
        }

        /// <summary>
        /// An annotation line, or a color, told apart by one character of lookahead.
        /// </summary>
        Token Hash()
        {
            var start = _ptr;
            _ptr++;

            if (!_ptr.IsOutOfBounds() && Hex(_ptr.Char) >= 0)
                return Colour(start);

            if (!_ptr.IsEndOfLine() && _ptr.Char != ' ')
                return Bad(start, "expected '# ' and an annotation, or a color, after '#'.");

            // One optional space is dropped, so a writer's "# " round-trips.
            if (_ptr.Char == ' ')
                _ptr++;

            var from = _ptr;
            _ptr = _ptr.FindEndOfLine();
            return new Token(TokenKind.Annotation, start.Index, Since(from));
        }

        Token Colour(TextPtr start)
        {
            long packed = 0;
            var digits = 0;

            // Walked as a local and written back once, measured: storing a TextPtr into a field pays a write barrier on
            // every step. Stepping the field made lexing 54% slower than stepping an integer; a local brings it to 11%.
            var ptr = _ptr;
            while (digits < 8)
            {
                var value = Hex(ptr.Char);
                if (value < 0)
                    break;

                packed = (packed << 4) | (uint)value;
                digits++;
                ptr++;
            }

            _ptr = ptr;

            if (digits == 6)
                return new Token(TokenKind.Color, start.Index, default(StringSegment), (packed << 8) | 0xFF);
            if (digits == 8)
                return new Token(TokenKind.Color, start.Index, default(StringSegment), packed);

            return Bad(start, $"expected six or eight hex digits in the color, not {digits}.");
        }

        /// <summary>
        /// Something in quotes, left where it lies unless something in it was doubled.
        /// </summary>
        Token Quoted(char quote, TokenKind kind)
        {
            var start = _ptr;
            _ptr++;

            var from = _ptr;
            var close = _ptr.FindInLine(quote);
            if (close.IsOutOfBounds())
            {
                _ptr = _ptr.FindEndOfLine();
                return Bad(start, Unclosed(kind));
            }

            if ((close + 1).Char == quote)
                return Doubled(start, from, quote, kind);

            _ptr = close;
            var text = Since(from);
            _ptr++;
            return new Token(kind, start.Index, Read(kind, text));
        }

        /// <summary>The rare value holding a doubled quote, built up a run at a time between them.</summary>
        Token Doubled(TextPtr start, TextPtr from, char quote, TokenKind kind)
        {
            var built = new StringBuilder();
            _ptr = from;

            while (true)
            {
                var close = _ptr.FindInLine(quote);
                if (close.IsOutOfBounds())
                {
                    _ptr = _ptr.FindEndOfLine();
                    return Bad(start, Unclosed(kind));
                }

                built.Append(_ptr.Content, _ptr.Index, close.Index - _ptr.Index);
                if ((close + 1).Char != quote)
                {
                    _ptr = close + 1;
                    return new Token(kind, start.Index, Read(kind, new StringSegment(built.ToString())));
                }

                built.Append(quote);
                _ptr = close + 2;
            }
        }

        /// <summary>Text with its entities read as what they stand for; a name, or text holding none, as it lies.</summary>
        static StringSegment Read(TokenKind kind, StringSegment text)
        {
            return kind == TokenKind.Text && TextEntities.Holds(text) ? new StringSegment(TextEntities.Read(text)) : text;
        }

        static string Unclosed(TokenKind kind)
        {
            return kind == TokenKind.Text ? "missing '\"' at the end of the text." : "missing ''' at the end of the name.";
        }

        /// <summary>
        /// A whole number, or a real told by a dot or an exponent.
        /// </summary>
        Token Number()
        {
            var start = _ptr;
            if (_ptr.Char == '-')
                _ptr++;

            var digits = Run();
            var real = false;

            if (_ptr.Char == '.' && Digit((_ptr + 1).Char))
            {
                real = true;
                _ptr++;
                Run();
            }

            if (_ptr.Char == 'e' || _ptr.Char == 'E')
            {
                var back = _ptr;
                _ptr++;
                if (_ptr.Char == '+' || _ptr.Char == '-')
                    _ptr++;

                if (Run() == 0)
                    _ptr = back;
                else
                    real = true;
            }

            var body = Since(start).ToString();
            if (digits == 0)
                return Bad(start, $"expected a number, not '{body}'.");

            if (real)
            {
                double value;
                if (!double.TryParse(body, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    return Bad(start, $"expected a number, not '{body}'.");

                return Letter(_ptr.Char)
                    ? Duration(start, 0, value, true)
                    : new Token(TokenKind.Real, start.Index, default(StringSegment), 0, value);
            }

            long whole;
            if (!long.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out whole))
                return Bad(start, $"'{body}' is too large for a whole number.");

            return Letter(_ptr.Char)
                ? Duration(start, whole, whole, false)
                : new Token(TokenKind.Integer, start.Index, default(StringSegment), whole);
        }

        /// <summary>
        /// A number that had a unit written against it: ticks, or seconds counted as the nearest whole tick.
        /// </summary>
        Token Duration(TextPtr start, long whole, double number, bool real)
        {
            var from = _ptr;

            // A local for the walk: stepping the field pays a write barrier per step.
            var ptr = _ptr;
            while (Letter(ptr.Char))
                ptr++;

            _ptr = ptr;

            var unit = Since(from);
            if (unit.Length == 1 && unit[0] == 't')
            {
                return real
                    ? Bad(start, $"expected a whole number of ticks, not '{Since(start)}'.")
                    : new Token(TokenKind.Duration, start.Index, unit, whole, number);
            }

            if (unit.Length != 1 || unit[0] != 's')
                return Bad(start, $"expected a duration like 600t or 2s, not '{Since(start)}'.");

            var ticks = number * Sequences.Duration.PerSecond;
            if (ticks > int.MaxValue || ticks < int.MinValue)
                return Bad(start, $"'{Since(start)}' is too long a time.");

            return new Token(TokenKind.Duration, start.Index, unit, Sequences.Duration.TicksIn(number), number);
        }

        int Run()
        {
            var ptr = _ptr;
            while (Digit(ptr.Char))
                ptr++;

            var digits = ptr.Index - _ptr.Index;
            _ptr = ptr;
            return digits;
        }

        /// <summary>
        /// A word starting with a letter, so a leading dash is always part of a number.
        /// </summary>
        Token Identifier()
        {
            var start = _ptr;
            var ptr = _ptr;
            while (true)
            {
                var c = ptr.Char;
                // No dash: a-1 is a subtraction.
                if (!Letter(c) && !Digit(c) && c != '_')
                    break;

                ptr++;
            }

            _ptr = ptr;
            var word = Since(start);
            var keyword = Keywords.Find(word);
            if (keyword != Keyword.None)
                return new Token(TokenKind.Keyword, start.Index, default(StringSegment), 0, 0, keyword);

            return new Token(TokenKind.Identifier, start.Index, word);
        }
    }
}
