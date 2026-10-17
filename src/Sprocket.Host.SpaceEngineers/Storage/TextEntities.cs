using System.Text;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>The two entities a program's text is written with: <c>&amp;br;</c> for a line break and <c>&amp;amp;</c> for an ampersand.</summary>
    /// <remarks>
    /// Text is quoted on one line, so a line break in it is written as an entity and read back as a break. An ampersand is
    /// written as <c>&amp;amp;</c> only where it would otherwise begin one of the two, so ordinary text keeps its ampersands as
    /// they are.
    /// </remarks>
    public static class TextEntities
    {
        const string Break = "&br;";
        const string Ampersand = "&amp;";

        /// <summary>Whether <paramref name="text" /> holds anything an entity stands for, so reading it needs <see cref="Read" />.</summary>
        public static bool Holds(StringSegment text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (text.Text[text.Start + i] == '&')
                    return true;
            }

            return false;
        }

        /// <summary><paramref name="text" /> as written in a program, its entities read as what they stand for.</summary>
        public static string Read(StringSegment text)
        {
            var built = new StringBuilder(text.Length);
            var end = text.Start + text.Length;
            for (var i = text.Start; i < end; i++)
            {
                if (text.Text[i] == '&' && At(text.Text, i, end, Break))
                {
                    built.Append('\n');
                    i += Break.Length - 1;
                    continue;
                }

                if (text.Text[i] == '&' && At(text.Text, i, end, Ampersand))
                {
                    built.Append('&');
                    i += Ampersand.Length - 1;
                    continue;
                }

                built.Append(text.Text[i]);
            }

            return built.ToString();
        }

        /// <summary>Appends the character of <paramref name="text" /> at <paramref name="at" /> to <paramref name="into" /> as a program writes it.</summary>
        /// <remarks>A carriage return is dropped, so a break typed as a return and a line feed is one break.</remarks>
        public static void Write(StringBuilder into, string text, int at)
        {
            var c = text[at];
            if (c == '\r')
                return;

            if (c == '\n')
            {
                into.Append(Break);
                return;
            }

            if (c == '&' && (At(text, at, text.Length, Break) || At(text, at, text.Length, Ampersand)))
            {
                into.Append(Ampersand);
                return;
            }

            into.Append(c);
        }

        /// <summary>Whether <paramref name="text" /> holds <paramref name="entity" /> at <paramref name="at" />, before <paramref name="end" />.</summary>
        static bool At(string text, int at, int end, string entity)
        {
            if (at + entity.Length > end)
                return false;

            return string.CompareOrdinal(text, at, entity, 0, entity.Length) == 0;
        }
    }
}
