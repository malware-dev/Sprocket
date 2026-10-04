using System.Globalization;
using System.Text;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// Turns script text into tokens, one at a time.
    /// </summary>
    /// <remarks>
    /// Built on the game's own <see cref="TextPtr" /> and <see cref="StringSegment" />, which exist for
    /// exactly this: a token is where it lies in the script, and nothing is copied out of the text until
    /// something asks for it. A keyword never is.
    /// <para>
    /// Two rules here are worth knowing before reading the code, because both exist to make a mistake loud
    /// rather than silent. <b>A quote is escaped by doubling it</b> and backslash means nothing, so a
    /// writer that forgets to escape cannot produce a file that still parses wrongly - a Windows path
    /// reading back as something else. And <b>every number is read with the invariant culture</b>, so a
    /// player whose decimal separator is a comma does not write <c>0,25</c> and read back <c>25</c> on
    /// somebody else's machine.
    /// </para>
    /// </remarks>
    sealed class Lexer
    {
        TextPtr _ptr;

        public Lexer(string text)
        {
            _ptr = new TextPtr(text ?? "");
        }

        /// <summary>The whole script, so trouble can be placed on a line after the fact.</summary>
        public string Script => _ptr.Content;

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
                case ':': return One(TokenKind.Colon);
                case '=': return Equality();
                case '#': return Hash();
                case '/': return Slashes();
                case '"': return Quoted('"', TokenKind.Text);
                case '\'': return Quoted('\'', TokenKind.Identifier);
            }

            if (c == '-' || Digit(c))
                return Number();

            if (Letter(c))
                return Identifier();

            return Bad(_ptr++, "unexpected '" + c + "'.");
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
            return new Token(TokenKind.EqualTo, start.Index);
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
        /// <remarks>
        /// Lookahead rather than position in the line, so the lexer stays context-free and a list holding a
        /// color is not a special case. The cost is one wart: a hash with no space after it is trouble
        /// rather than an annotation. A writer always emits the space.
        /// </remarks>
        Token Hash()
        {
            var start = _ptr;
            _ptr++;

            if (!_ptr.IsOutOfBounds() && Hex(_ptr.Char) >= 0)
                return Colour(start);

            if (!_ptr.IsEndOfLine() && _ptr.Char != ' ')
                return Bad(start, "expected '# ' and an annotation, or a color, after '#'.");

            // Everything after the hash and one optional space, so a writer emitting "# " round-trips.
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

            // Walked as a local. Stepping the cursor in its field pays a write barrier per digit,
            // which is per digit of every color in the file - see Run, which does the same.
            // Walked as a local and written back once, measured: a TextPtr holds a string, so storing one into a
            // field pays the collector's write barrier on every step. Stepping the field made lexing 54% slower
            // than stepping an integer; walking a local brings it to 11%.
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

            return Bad(start, "expected six or eight hex digits in the color, not " + digits + ".");
        }

        /// <summary>
        /// Something in quotes, left where it lies unless something in it was doubled.
        /// </summary>
        /// <remarks>
        /// Double quotes are text and single quotes are a name, and each escapes only its own quote, by
        /// doubling it. Most values in a save are quoted and almost none contain their own quote, so the
        /// ordinary path finds the closing one and keeps a segment, and only a value that actually holds a
        /// doubled quote has a new string built.
        /// </remarks>
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
            return new Token(kind, start.Index, text);
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
                    return new Token(kind, start.Index, new StringSegment(built.ToString()));
                }

                built.Append(quote);
                _ptr = close + 2;
            }
        }

        static string Unclosed(TokenKind kind)
        {
            return kind == TokenKind.Text ? "missing '\"' at the end of the text." : "missing ''' at the end of the name.";
        }

        /// <summary>
        /// A whole number, or a real told by a dot or an exponent.
        /// </summary>
        /// <remarks>
        /// Scientific notation is forced rather than chosen: a writer emitting a double so that it can be read
        /// back exactly produces <c>1E-05</c> on its own, so refusing it would mean refusing what Sprocket wrote.
        /// </remarks>
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
                return Bad(start, "expected a number, not '" + body + "'.");

            if (real)
            {
                double value;
                if (!double.TryParse(body, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    return Bad(start, "expected a number, not '" + body + "'.");

                return new Token(TokenKind.Real, start.Index, default(StringSegment), 0, value);
            }

            long whole;
            if (!long.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out whole))
                return Bad(start, "'" + body + "' is too large for a whole number.");

            return Letter(_ptr.Char)
                ? Duration(start, whole)
                : new Token(TokenKind.Integer, start.Index, default(StringSegment), whole);
        }

        /// <summary>
        /// A number that had a unit written against it, as ticks.
        /// </summary>
        /// <remarks>
        /// Ticks are the only unit, so anything else is trouble rather than a number followed by a word:
        /// <c>600s</c> is somebody meaning seconds, and reading it as 600 would run their airlock ten times
        /// too fast.
        /// </remarks>
        Token Duration(TextPtr start, long ticks)
        {
            var from = _ptr;

            // A local for the walk, like Run and Colour: the field is what a write barrier watches.
            var ptr = _ptr;
            while (Letter(ptr.Char))
                ptr++;

            _ptr = ptr;

            var unit = Since(from);
            if (unit.Length != 1 || unit[0] != 't')
                return Bad(start, "expected a duration like 600t, not '" + Since(start) + "'.");

            return new Token(TokenKind.Duration, start.Index, default(StringSegment), ticks);
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
        /// <remarks>
        /// Checked against the format's own words where it lies. A keyword comes back as a number with no
        /// text at all.
        /// </remarks>
        Token Identifier()
        {
            var start = _ptr;
            var ptr = _ptr;
            while (true)
            {
                var c = ptr.Char;
                // No dash. It is subtraction, and a name that could hold one would make a-1 a name
                // and a - 1 a subtraction, which is the same text meaning two things over a space.
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
