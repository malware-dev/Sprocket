using System.Collections.Generic;
using System.Globalization;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Values;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// A colour as the separate numbers an action takes, and back again.
    /// </summary>
    /// <remarks>
    /// The two halves of the same claim, which is that these values are one colour. Somebody wrote
    /// that down in a description and nothing else in the game says it: an action declares how many
    /// values it wants and not one word about what they are for.
    /// <para>
    /// Written here rather than in the control that chooses a colour, because the control deals in
    /// colours and should never learn that a light takes three numbers. This is the seam between the
    /// two, and it is the only place that knows both.
    /// </para>
    /// </remarks>
    static class Channels
    {
        /// <summary>
        /// The colour <paramref name="count" /> values starting at <paramref name="from" /> make,
        /// as the language writes one, or empty where they do not make one yet.
        /// </summary>
        /// <remarks>
        /// Empty rather than black where the values were never filled in. A step somebody has not
        /// finished writing has no colour, and opening its picker on black would be an answer it
        /// never gave.
        /// </remarks>
        public static string Said(IList<Literal> values, int from, int count)
        {
            if (count == 1)
                return from < values.Count ? values[from].Said : "";

            var parts = new byte[3];
            for (var i = 0; i < 3; i++)
            {
                if (from + i >= values.Count)
                    return "";

                Literal value = values[from + i];
                if (value.Kind != ValueKind.Integer)
                    return "";

                long whole = value.Whole;
                parts[i] = (byte)(whole < 0 ? 0 : whole > 255 ? 255 : whole);
            }

            return "#" + Hex(parts[0]) + Hex(parts[1]) + Hex(parts[2]);
        }

        /// <summary>
        /// What each field shows for the values behind it, one per field.
        /// </summary>
        /// <remarks>
        /// Not one per value: a field can stand over several of them, and can show something other
        /// than the value itself. Here rather than in either caller, because a step's own chip and
        /// the question that fills a step in have to arrive at the same words - a value that read
        /// one way in a sentence and another in the box that sets it would be two values.
        /// </remarks>
        public static void Shown(IList<Literal> values, IList<Field> fields, IList<string> into)
        {
            var at = 0;
            for (var i = 0; i < fields.Count; i++)
            {
                into.Add(fields[i].Wording(Said(values, at, fields[i].Covers)));
                at += fields[i].Covers;
            }
        }

        /// <summary>
        /// A written colour as the <paramref name="count" /> values the action wants, added in order.
        /// </summary>
        /// <remarks>
        /// In the order the group declared them, which is the whole of what the order means: three
        /// parameters under a colour are its red, its green and its blue, because that is what a
        /// colour of three numbers is. Alpha is not among them - nothing that takes three wants it.
        /// </remarks>
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
        /// The colour a written one says, and how solid it is, or nothing where it is not one yet.
        /// </summary>
        /// <remarks>
        /// Six or eight hex digits behind a hash, and nothing else. The one reader of a written
        /// colour there is, so the picker, the sentence and this all agree on what counts as one -
        /// they did not when there were two of these.
        /// </remarks>
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
