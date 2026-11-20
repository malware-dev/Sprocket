using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Text
{
    /// <summary>
    /// A script as the pieces the lexer reads it into, each in as few bytes as its kind allows: a keyword or a symbol in one
    /// byte, a number packed, a quoted id in its 16 bytes, a name or text spelled out once and pointed back to after. Only what
    /// the script says is kept, not how it is laid out: it comes back with one space between every two pieces, which a reader
    /// takes the same.
    /// </summary>
    /// <remarks>Members, Sprocket's own and those mods add alike, are names like any other.</remarks>
    public static class TokenCode
    {
        const byte Newline = 1;
        const byte Spelled = 2;
        const byte Integer = 3;
        const byte Identity = 4;
        const byte Name = 5;
        const byte Quoted = 6;
        const byte Again = 7;
        const byte Real = 8;
        const byte Ticks = 9;
        const byte Seconds = 10;
        const byte Color = 11;
        const byte FirstSpelling = 16;

        /// <summary>The words and symbols coded in one byte, the first coded as 16 and each after it one more.</summary>
        /// <remarks>Part of the shared text's format: an entry is never moved or removed, and a new one goes at the end.</remarks>
        public static readonly string[] Spellings =
        {
            "{", "}", "[", "]", "(", ")", ",", ".", ":", "=", "|", "//", "#@",
            "sprocket", "block", "system", "instance", "of", "sequence",
            "when", "every", "then", "trigger", "to", "run", "set",
            "on", "off", "all", "any", "wait", "await", "required", "expects", "changed", "as", "last",
            "running", "step", "stop", "call", "alpha", "optional", "let", "blueprint", "slot", "if", "else", "==",
            "signal", "sum", "!=", "<", ">", "<=", ">=", "whenever", "from", "average", "type",
            "is", "not", "installed"
        };

        /// <summary>The bytes of <paramref name="script" />.</summary>
        public static byte[] Encode(string script)
        {
            var bytes = new List<byte>(script.Length);
            var seen = new List<string>();
            foreach (var piece in Pieces(script))
            {
                if (piece.Token.Kind == TokenKind.Newline)
                    bytes.Add(Newline);
                else
                    Write(bytes, piece.Token, piece.Spelled, seen);
            }

            return bytes.ToArray();
        }

        /// <summary>The script <paramref name="bytes" /> hold, or null where they are damaged.</summary>
        public static string Decode(byte[] bytes)
        {
            var script = new StringBuilder(bytes.Length * 3);
            var seen = new List<string>();
            var lineStart = true;
            var at = 0;
            while (at < bytes.Length)
            {
                var code = bytes[at++];
                if (code == Newline)
                {
                    script.Append('\n');
                    lineStart = true;
                    continue;
                }

                var spelled = Read(code, bytes, ref at, seen);
                if (spelled == null)
                    return null;

                if (!lineStart)
                    script.Append(' ');
                script.Append(spelled);
                lineStart = false;
            }

            return script.ToString();
        }

        /// <summary>The pieces of <paramref name="script" />, each spelled as written.</summary>
        static List<Piece> Pieces(string script)
        {
            var pieces = new List<Piece>();
            var lexer = new Lexer(script);
            var token = lexer.Next();
            while (token.Kind != TokenKind.End)
            {
                var next = lexer.Next();
                var end = next.Kind == TokenKind.End ? script.Length : next.At;
                pieces.Add(new Piece(token, script.Substring(token.At, end - token.At).TrimEnd(' ', '\t', '\r', '\n')));
                token = next;
            }

            return pieces;
        }

        static void Write(List<byte> bytes, Token token, string spelled, List<string> seen)
        {
            // A bare name spelled like a contextual keyword is coded as the keyword is, and reads back as it was.
            var kind = token.Kind;
            var keyworded = kind == TokenKind.Identifier && token.Keyword != Keyword.None;
            if (keyworded || (kind != TokenKind.Identifier && kind != TokenKind.Text && kind != TokenKind.Annotation && kind != TokenKind.Bad))
            {
                var index = Array.IndexOf(Spellings, spelled);
                if (index >= 0)
                {
                    bytes.Add((byte)(FirstSpelling + index));
                    return;
                }
            }

            // Literals as their values; they come back as the writer spells them.
            switch (kind)
            {
                case TokenKind.Integer:
                    bytes.Add(Integer);
                    WriteNumber(bytes, Zigzag(token.Integer));
                    return;

                case TokenKind.Real:
                    if (WriteReal(bytes, Real, token.Real))
                        return;
                    break;

                case TokenKind.Duration:
                    if (spelled.EndsWith("t", StringComparison.Ordinal))
                    {
                        bytes.Add(Ticks);
                        WriteNumber(bytes, Zigzag(token.Integer));
                        return;
                    }

                    if (WriteReal(bytes, Seconds, token.Real))
                        return;
                    break;

                case TokenKind.Color:
                    bytes.Add(Color);
                    for (var shift = 24; shift >= 0; shift -= 8)
                        bytes.Add((byte)(token.Integer >> shift));
                    return;
            }

            Guid identity;
            if (kind == TokenKind.Text && spelled.Length == 38 && Guid.TryParseExact(spelled.Substring(1, 36), "D", out identity) &&
                "\"" + identity.ToString("D") + "\"" == spelled)
            {
                bytes.Add(Identity);
                bytes.AddRange(identity.ToByteArray());
                return;
            }

            // Anything spelled out is pointed back to the next time, by how recently it was last written.
            var again = seen.IndexOf(spelled);
            if (again >= 0)
            {
                seen.RemoveAt(again);
                seen.Insert(0, spelled);
                bytes.Add(Again);
                WriteNumber(bytes, again);
                return;
            }

            seen.Insert(0, spelled);
            var code = Spelled;
            var inner = spelled;
            if (spelled.Length >= 2 && spelled[spelled.Length - 1] == spelled[0] && (spelled[0] == '\'' || spelled[0] == '"') &&
                (kind == TokenKind.Identifier || kind == TokenKind.Text))
            {
                code = spelled[0] == '\'' ? Name : Quoted;
                inner = spelled.Substring(1, spelled.Length - 2);
            }

            var raw = Utf8.Bytes(inner);
            bytes.Add(code);
            WriteNumber(bytes, raw.Length);
            bytes.AddRange(raw);
        }

        static string Read(byte code, byte[] bytes, ref int at, List<string> seen)
        {
            if (code >= FirstSpelling)
                return code - FirstSpelling < Spellings.Length ? Spellings[code - FirstSpelling] : null;

            long number;
            string real;
            switch (code)
            {
                case Integer:
                    return ReadNumber(bytes, ref at, out number) ? Unzigzag(number).ToString(CultureInfo.InvariantCulture) : null;

                case Ticks:
                    return ReadNumber(bytes, ref at, out number) ? Unzigzag(number).ToString(CultureInfo.InvariantCulture) + "t" : null;

                case Real:
                    return ReadReal(bytes, ref at);

                case Seconds:
                    real = ReadReal(bytes, ref at);
                    return real == null ? null : real + "s";

                case Color:
                    if (at + 4 > bytes.Length)
                        return null;
                    var color = "#" + bytes[at].ToString("X2") + bytes[at + 1].ToString("X2") + bytes[at + 2].ToString("X2") + bytes[at + 3].ToString("X2");
                    at += 4;
                    return color;

                case Identity:
                    if (at + 16 > bytes.Length)
                        return null;
                    var raw = new byte[16];
                    Array.Copy(bytes, at, raw, 0, 16);
                    at += 16;
                    return "\"" + new Guid(raw).ToString("D") + "\"";

                case Again:
                    if (!ReadNumber(bytes, ref at, out number) || number >= seen.Count)
                        return null;
                    var again = seen[(int)number];
                    seen.RemoveAt((int)number);
                    seen.Insert(0, again);
                    return again;

                case Spelled:
                case Name:
                case Quoted:
                    if (!ReadNumber(bytes, ref at, out number) || number > bytes.Length - at)
                        return null;
                    var text = new byte[number];
                    Array.Copy(bytes, at, text, 0, (int)number);
                    at += (int)number;
                    var inner = Utf8.Text(text);
                    if (inner == null)
                        return null;

                    var spelled = code == Name ? "'" + inner + "'" : code == Quoted ? "\"" + inner + "\"" : inner;
                    seen.Insert(0, spelled);
                    return spelled;

                default:
                    return null;
            }
        }

        static long Zigzag(long number) => (number << 1) ^ (number >> 63);

        static long Unzigzag(long number) => (long)((ulong)number >> 1) ^ -(number & 1);

        /// <summary>
        /// Writes <paramref name="code" /> and <paramref name="value" /> as a decimal floating-point number: its significant
        /// digits as one integer and the power of ten they are scaled by, from the shortest text that reads back as the same
        /// double. Answers false, writing nothing, for a value that has no such text: NaN, the infinities and -0.
        /// </summary>
        static bool WriteReal(List<byte> bytes, byte code, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || (value == 0 && 1 / value < 0))
                return false;

            // "R" fails to round-trip now and then on .NET Framework; "G17" always does.
            var text = value.ToString("R", CultureInfo.InvariantCulture);
            double back;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out back) || back != value)
                text = value.ToString("G17", CultureInfo.InvariantCulture);

            var exponent = 0;
            var e = text.IndexOfAny(new[] { 'E', 'e' });
            if (e >= 0)
            {
                exponent = int.Parse(text.Substring(e + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                text = text.Substring(0, e);
            }

            var negative = text.StartsWith("-", StringComparison.Ordinal);
            if (negative)
                text = text.Substring(1);

            var point = text.IndexOf('.');
            if (point >= 0)
            {
                exponent -= text.Length - point - 1;
                text = text.Remove(point, 1);
            }

            text = text.TrimStart('0');
            while (text.Length > 0 && text[text.Length - 1] == '0')
            {
                text = text.Substring(0, text.Length - 1);
                exponent++;
            }

            var digits = text.Length == 0 ? 0 : long.Parse(text, CultureInfo.InvariantCulture);
            bytes.Add(code);
            WriteNumber(bytes, Zigzag(negative ? -digits : digits));
            WriteNumber(bytes, Zigzag(text.Length == 0 ? 0 : exponent));
            return true;
        }

        /// <summary>The real at <paramref name="at" />, as the writer spells it, or null where the bytes are damaged.</summary>
        static string ReadReal(byte[] bytes, ref int at)
        {
            long digits, exponent;
            if (!ReadNumber(bytes, ref at, out digits) || !ReadNumber(bytes, ref at, out exponent))
                return null;

            double value;
            var text = Unzigzag(digits).ToString(CultureInfo.InvariantCulture) + "E" + Unzigzag(exponent).ToString(CultureInfo.InvariantCulture);
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                ? value.ToString("R", CultureInfo.InvariantCulture)
                : null;
        }

        static void WriteNumber(List<byte> bytes, long number)
        {
            var left = (ulong)number;
            do
            {
                var low = (byte)(left & 0x7F);
                left >>= 7;
                bytes.Add(left != 0 ? (byte)(low | 0x80) : low);
            } while (left != 0);
        }

        static bool ReadNumber(byte[] bytes, ref int at, out long number)
        {
            ulong read = 0;
            for (var shift = 0; shift < 64; shift += 7)
            {
                if (at >= bytes.Length)
                    break;

                var next = bytes[at++];
                read |= (ulong)(next & 0x7F) << shift;
                if ((next & 0x80) == 0)
                {
                    number = (long)read;
                    return number >= 0;
                }
            }

            number = 0;
            return false;
        }
    }
}
