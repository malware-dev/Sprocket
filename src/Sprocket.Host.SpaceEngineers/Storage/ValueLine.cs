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
    /// The same spellings the language uses, so what somebody types into a field is what their
    /// program says afterwards: a whole number, a real, <c>on</c> and <c>off</c> for a flag,
    /// <c>#FF8800</c> for a colour, and double quotes around text that holds a space.
    /// <para>
    /// Read here rather than through the lexer because this is one short line typed by a person, not
    /// a document: the lexer works on a whole program and answers with statements, and what is
    /// wanted is a handful of values with no structure around them. What it does share is the
    /// spelling, which is the part that has to agree.
    /// </para>
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
                    // Quoted as a program quotes text, so its entities are read back as what they stand for: a line break
                    // written as one comes back a break.
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
        /// <remarks>
        /// The language's own rule, and the reason it is the language's rule: no character changes
        /// the meaning of the next one, so text holding a quote cannot come back as something else.
        /// </remarks>
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
        /// <remarks>
        /// A word that spells nothing else is text. Somebody typing the name of a mode into a field
        /// meant the word, and refusing it would be Sprocket claiming to know what the action wanted
        /// when the game has told it nothing at all.
        /// </remarks>
        static Literal Bare(string word)
        {
            if (word == "on")
                return Literal.Of(true);

            if (word == "off")
                return Literal.Of(false);

            if (word.Length > 1 && word[0] == '#')
                return Colour(word);

            long whole;
            if (long.TryParse(word, NumberStyles.Integer, CultureInfo.InvariantCulture, out whole))
                return Literal.Of(whole);

            double real;
            if (double.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out real))
                return Literal.Of(real);

            return Literal.Of(word);
        }

        /// <summary>Six hex digits, or eight where the last two are how solid it is.</summary>
        static Literal Colour(string word)
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

            return Literal.Colour(bytes[0], bytes[1], bytes[2], bytes[3]);
        }
    }
}
