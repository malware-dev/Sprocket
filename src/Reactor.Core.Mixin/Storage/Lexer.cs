using System.Globalization;
using System.Text;

namespace Reactor.Storage
{
    /// <summary>
    /// Turns script text into tokens, one at a time.
    /// </summary>
    /// <remarks>
    /// Hand-rolled because regular expressions are not on the mod whitelist, and small enough that
    /// they would not have earned their place anyway.
    /// <para>
    /// Two rules here are worth knowing before reading the code, because both exist to make a mistake
    /// loud rather than silent. <b>A string escapes a quote by doubling it</b> and backslash means
    /// nothing, so a writer that forgets to escape cannot produce a file that still parses wrongly - a
    /// Windows path reading back as something else. And <b>everything is read with the invariant
    /// culture</b>, so a player whose decimal separator is a comma does not write <c>0,25</c> and read
    /// back <c>25</c> on somebody else's machine.
    /// </para>
    /// </remarks>
    sealed class Lexer
    {
        readonly string _text;
        int _at;
        int _line = 1;

        public Lexer(string text)
        {
            _text = text ?? "";
        }

        public Token Next()
        {
            while (_at < _text.Length && (_text[_at] == ' ' || _text[_at] == '\t'))
                _at++;

            if (_at >= _text.Length)
                return new Token(TokenKind.End, _line);

            var c = _text[_at];
            if (c == '\r' || c == '\n')
                return Ending();

            switch (c)
            {
                case '{': return One(TokenKind.BlockOpen);
                case '}': return One(TokenKind.BlockClose);
                case '[': return One(TokenKind.ListOpen);
                case ']': return One(TokenKind.ListClose);
                case '(': return One(TokenKind.VectorOpen);
                case ')': return One(TokenKind.VectorClose);
                case ',': return One(TokenKind.Comma);
                case '#': return Hash();
                case '"': return Quoted();
            }

            if (c == '-' || Digit(c))
                return Number();

            if (Letter(c))
                return Identifier();

            _at++;
            return Bad("'" + c + "' means nothing here");
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
            _at++;
            return new Token(kind, _line);
        }

        Token Bad(string what)
        {
            return new Token(TokenKind.Bad, _line, what);
        }

        /// <summary>A newline, counted after the token, so the token belongs to the line it ended.</summary>
        Token Ending()
        {
            var at = _line;
            if (_text[_at] == '\r')
                _at++;
            if (_at < _text.Length && _text[_at] == '\n')
                _at++;

            _line++;
            return new Token(TokenKind.Newline, at);
        }

        /// <summary>
        /// An annotation line, or a colour, told apart by one character of lookahead.
        /// </summary>
        /// <remarks>
        /// Lookahead rather than position in the line, so the lexer stays context-free and a list
        /// holding a colour is not a special case. The cost is one wart: a hash with no space after it
        /// is trouble rather than an annotation. A writer always emits the space.
        /// </remarks>
        Token Hash()
        {
            var line = _line;
            _at++;

            if (_at < _text.Length && Hex(_text[_at]) >= 0)
                return Colour(line);

            if (_at < _text.Length && _text[_at] != ' ' && _text[_at] != '\r' && _text[_at] != '\n')
                return Bad("a '#' starts an annotation or a colour, and this is neither");

            // Everything after the hash and one optional space, so a writer emitting "# " round-trips.
            if (_at < _text.Length && _text[_at] == ' ')
                _at++;

            var from = _at;
            while (_at < _text.Length && _text[_at] != '\r' && _text[_at] != '\n')
                _at++;

            return new Token(TokenKind.Annotation, line, _text.Substring(from, _at - from));
        }

        Token Colour(int line)
        {
            long packed = 0;
            var digits = 0;
            while (_at < _text.Length && digits < 8)
            {
                var value = Hex(_text[_at]);
                if (value < 0)
                    break;

                packed = (packed << 4) | (uint)value;
                digits++;
                _at++;
            }

            if (digits == 6)
                return new Token(TokenKind.Color, line, null, (packed << 8) | 0xFF);
            if (digits == 8)
                return new Token(TokenKind.Color, line, null, packed);

            return Bad("a colour is six or eight hex digits, not " + digits);
        }

        Token Quoted()
        {
            var line = _line;
            _at++;

            var word = new StringBuilder();
            while (true)
            {
                if (_at >= _text.Length || _text[_at] == '\r' || _text[_at] == '\n')
                    return Bad("a text value is not closed");

                if (_text[_at] != '"')
                {
                    word.Append(_text[_at]);
                    _at++;
                    continue;
                }

                _at++;
                if (_at < _text.Length && _text[_at] == '"')
                {
                    word.Append('"');
                    _at++;
                    continue;
                }

                return new Token(TokenKind.Text, line, word.ToString());
            }
        }

        /// <summary>
        /// A whole number, or a real told by a dot or an exponent.
        /// </summary>
        /// <remarks>
        /// Scientific notation is forced rather than chosen: a writer emitting a double so that it can
        /// be read back exactly produces <c>1E-05</c> on its own, so refusing it would mean refusing
        /// what we wrote.
        /// </remarks>
        Token Number()
        {
            var line = _line;
            var from = _at;
            if (_text[_at] == '-')
                _at++;

            var digits = Run();
            var real = false;

            if (_at + 1 < _text.Length && _text[_at] == '.' && Digit(_text[_at + 1]))
            {
                real = true;
                _at++;
                Run();
            }

            if (_at < _text.Length && (_text[_at] == 'e' || _text[_at] == 'E'))
            {
                var back = _at;
                _at++;
                if (_at < _text.Length && (_text[_at] == '+' || _text[_at] == '-'))
                    _at++;

                if (Run() == 0)
                    _at = back;
                else
                    real = true;
            }

            var body = _text.Substring(from, _at - from);
            if (digits == 0)
                return Bad("'" + body + "' is not a number");

            if (real)
            {
                double value;
                if (!double.TryParse(body, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    return Bad("'" + body + "' is not a number");

                return new Token(TokenKind.Real, line, null, 0, value);
            }

            long whole;
            if (!long.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out whole))
                return Bad("'" + body + "' does not fit in a whole number");

            return new Token(TokenKind.Integer, line, null, whole);
        }

        int Run()
        {
            var from = _at;
            while (_at < _text.Length && Digit(_text[_at]))
                _at++;

            return _at - from;
        }

        /// <summary>A word starting with a letter, so a leading dash is always part of a number.</summary>
        Token Identifier()
        {
            var line = _line;
            var from = _at;
            while (_at < _text.Length)
            {
                var c = _text[_at];
                if (!Letter(c) && !Digit(c) && c != '_' && c != '-')
                    break;

                _at++;
            }

            return new Token(TokenKind.Identifier, line, _text.Substring(from, _at - from));
        }
    }
}
