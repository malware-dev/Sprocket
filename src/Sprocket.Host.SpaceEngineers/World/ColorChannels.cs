using System.Collections.Generic;
using System.Globalization;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Fields;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// A color as the separate numbers an action takes, and back again.
    /// </summary>
    public static class ColorChannels
    {
        /// <summary>
        /// The color <paramref name="count" /> values starting at <paramref name="from" /> make,
        /// as the language writes one, or empty where they do not make one yet.
        /// </summary>
        public static string Said(IList<Literal> arguments, int from, int count)
        {
            if (count == 1)
                return from < arguments.Count ? arguments[from].Said : "";

            var parts = new byte[3];
            for (var i = 0; i < 3; i++)
            {
                if (from + i >= arguments.Count)
                    return "";

                Literal value = arguments[from + i];
                if (value.Kind != ValueKind.Integer)
                    return "";

                long whole = value.Whole;
                parts[i] = (byte)(whole < 0 ? 0 : whole > 255 ? 255 : whole);
            }

            return $"#{Hex(parts[0])}{Hex(parts[1])}{Hex(parts[2])}";
        }

        /// <summary>
        /// What each field shows for the values behind it, one per field.
        /// </summary>
        public static void Shown(IList<Literal> arguments, IList<Field> fields, IList<string> into)
        {
            var at = 0;
            for (var i = 0; i < fields.Count; i++)
            {
                // A value left out is the one the action does without it, which an optional field starts with.
                var said = Said(arguments, at, fields[i].Covers);
                into.Add(said.Length == 0 && fields[i].IsOptional
                    ? fields[i].Default
                    : fields[i].Wording(said));
                at += fields[i].Covers;
            }
        }

        /// <summary>
        /// A written color as the <paramref name="count" /> values the action wants, added in order.
        /// </summary>
        /// <remarks>Three values are red, green and blue, in that order.</remarks>
        public static void Spell(string said, int count, IList<string> into)
        {
            if (count == 1)
            {
                into.Add(said ?? "");
                return;
            }

            Color shade;
            byte alpha;
            if (!Colour(said, out shade, out alpha))
            {
                for (var i = 0; i < count; i++)
                    into.Add("");

                return;
            }

            into.Add(Whole(shade.R));
            into.Add(Whole(shade.G));
            into.Add(Whole(shade.B));

            for (var i = 3; i < count; i++)
                into.Add("");
        }

        /// <summary>
        /// The color a written one says, and how solid it is, or nothing where it is not one yet.
        /// </summary>
        /// <remarks>A written color is six or eight hex digits behind a hash.</remarks>
        public static bool Colour(string said, out Color colour, out byte alpha)
        {
            colour = Color.White;
            alpha = 255;

            if (said == null || said.Length < 7 || said[0] != '#')
                return false;

            string digits = said.Substring(1);
            if (digits.Length != 6 && digits.Length != 8)
                return false;

            byte r, g, b, a = 255;
            if (!Part(digits, 0, out r) || !Part(digits, 1, out g) || !Part(digits, 2, out b))
                return false;

            if (digits.Length == 8 && !Part(digits, 3, out a))
                return false;

            colour = new Color(r, g, b);
            alpha = a;
            return true;
        }

        /// <summary>
        /// <paramref name="color" /> written as the language writes a color: six hex digits behind a hash, and two more for its
        /// alpha where it is not solid.
        /// </summary>
        public static string Code(Color color)
        {
            var said = $"#{Hex(color.R)}{Hex(color.G)}{Hex(color.B)}";
            return color.A == 255 ? said : said + Hex(color.A);
        }

        static bool Part(string digits, int at, out byte value)
        {
            return byte.TryParse(digits.Substring(at * 2, 2), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out value);
        }

        static string Hex(byte part)
        {
            return part.ToString("X2", CultureInfo.InvariantCulture);
        }

        static string Whole(byte part)
        {
            return part.ToString(CultureInfo.InvariantCulture);
        }
    }
}
