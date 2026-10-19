using System;
using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>How far apart two colors stand to the eye, by the contrast ratio of the Web Content Accessibility Guidelines.</summary>
    /// <remarks>
    /// The ratio runs from 1, two colors alike, to 21, black on white. Text wants 4.5 against what is behind it, and a mark
    /// or large text 3.
    /// </remarks>
    public static class Contrast
    {
        /// <summary>The ratio text is read at.</summary>
        public const float Text = 4.5f;

        /// <summary>The ratio a mark, or large text, is seen at.</summary>
        public const float Mark = 3f;

        /// <summary>How far apart <paramref name="first" /> and <paramref name="second" /> stand, from 1 to 21.</summary>
        public static float Ratio(Color first, Color second)
        {
            var one = Luminance(first);
            var other = Luminance(second);
            return (Math.Max(one, other) + 0.05f) / (Math.Min(one, other) + 0.05f);
        }

        /// <summary>
        /// <paramref name="color" />, moved toward <paramref name="toward" /> by as little as brings it <paramref name="ratio" />
        /// apart from <paramref name="ground" />, or <paramref name="toward" /> itself where nothing short of it does.
        /// </summary>
        public static Color Separate(Color color, Color ground, Color toward, float ratio)
        {
            for (var step = 0; step <= 10; step++)
            {
                var moved = Color.Lerp(color, toward, step / 10f);
                if (Ratio(moved, ground) >= ratio)
                    return moved;
            }

            return toward;
        }

        /// <summary>How bright <paramref name="color" /> is to the eye, from 0 for black to 1 for white.</summary>
        static float Luminance(Color color)
        {
            return 0.2126f * Linear(color.R) + 0.7152f * Linear(color.G) + 0.0722f * Linear(color.B);
        }

        static float Linear(byte channel)
        {
            var value = channel / 255f;
            return value <= 0.04045f ? value / 12.92f : (float)Math.Pow((value + 0.055f) / 1.055f, 2.4f);
        }
    }
}
