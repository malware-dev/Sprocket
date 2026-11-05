using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// A line of values as somebody typed it, read into the values they wrote.
    /// </summary>
    /// <remarks>
    /// The spellings the language uses: a whole number, a real, <c>on</c> and <c>off</c> for a flag, <c>#FF8800</c> for a
    /// color, and double quotes around text that holds a space.
    /// </remarks>
    public static class ValueLine
    {
        /// <summary>Reads every value on the line, in order, appending them.</summary>
        public static void Read(string said, List<Literal> into)
        {
            if (string.IsNullOrEmpty(said))
                return;

            var at = 0;
            while (at < said.Length)
            {
                while (at < said.Length && said[at] == ' ')
                    at++;

                if (at >= said.Length)
                    return;

                if (said[at] == '"')
                {
                    // Quoted as a program quotes text, so its entities are read back as what they stand for.
                    var text = Quoted(said, ref at);
                    into.Add(Literal.Of(text.IndexOf('&') < 0 ? text : TextEntities.Read(new StringSegment(text))));
                    continue;
                }

                var from = at;
                while (at < said.Length && said[at] != ' ')
                    at++;

                into.Add(Bare(said.Substring(from, at - from)));
            }
        }

        /// <summary>
        /// Text between double quotes, with a doubled quote standing for one.
        /// </summary>
        static string Quoted(string said, ref int at)
        {
            at++;

            var text = "";
            while (at < said.Length)
            {
                if (said[at] != '"')
                {
                    text += said[at++];
                    continue;
                }

                at++;
                if (at < said.Length && said[at] == '"')
                {
                    text += '"';
                    at++;
                    continue;
                }

                return text;
            }

            return text;
        }

        /// <summary>
        /// One unquoted word, as whatever it spells.
        /// </summary>
        /// <remarks>A word that spells nothing else is text.</remarks>
        static Literal Bare(string word)
        {
            if (word == "on")
                return Literal.Of(true);

            if (word == "off")
                return Literal.Of(false);

            if (word.Length > 1 && word[0] == '#')
                return Color(word);

            long whole;
            if (long.TryParse(word, NumberStyles.Integer, CultureInfo.InvariantCulture, out whole))
                return Literal.Of(whole);

            double real;
            if (double.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out real))
                return Literal.Of(real);

            return Literal.Of(word);
        }

        /// <summary>Six hex digits, or eight where the last two are how solid it is.</summary>
        static Literal Color(string word)
        {
            var digits = word.Substring(1);
            if (digits.Length != 6 && digits.Length != 8)
                return Literal.Of(word);

            var bytes = new byte[4];
            bytes[3] = 255;

            for (var i = 0; i * 2 < digits.Length; i++)
            {
                byte value;
                if (!byte.TryParse(digits.Substring(i * 2, 2), NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out value))
                {
                    return Literal.Of(word);
                }

                bytes[i] = value;
            }

            return Literal.Color(bytes[0], bytes[1], bytes[2], bytes[3]);
        }
    }
}
