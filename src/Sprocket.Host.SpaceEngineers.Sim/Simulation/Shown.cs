using System.Globalization;
using VRageMath;

namespace Sprocket.Sim.Simulation
{
    /// <summary>
    /// How a frame writes the game's own maths types out.
    /// </summary>
    /// <remarks>
    /// Spelled here rather than left to each type's <c>ToString</c>, because a reference frame is
    /// compared byte for byte: the format is part of what the frames mean, and it must not be
    /// something the game could change underneath them. Invariant culture for the same reason.
    /// </remarks>
    static class Shown
    {
        public static string Say(Vector2 value)
        {
            return "{X:" + Say(value.X) + " Y:" + Say(value.Y) + "}";
        }

        public static string Say(Rectangle value)
        {
            return "{X:" + value.X.ToString(CultureInfo.InvariantCulture)
                + " Y:" + value.Y.ToString(CultureInfo.InvariantCulture)
                + " W:" + value.Width.ToString(CultureInfo.InvariantCulture)
                + " H:" + value.Height.ToString(CultureInfo.InvariantCulture) + "}";
        }

        public static string Say(RectangleF value)
        {
            return "{X:" + Say(value.X) + " Y:" + Say(value.Y)
                + " W:" + Say(value.Width) + " H:" + Say(value.Height) + "}";
        }

        /// <summary>
        /// A number, to five decimals, with nothing said about how it is held.
        /// </summary>
        /// <remarks>
        /// Spelled out rather than left to the framework, which prints the same float differently on
        /// .NET Framework and on .NET - seven significant digits against the shortest that round-trips.
        /// A frame is compared byte for byte, so that difference would read as the screen having moved.
        /// <para>
        /// Five decimals is a thousandth of a pixel: far past anything a layout decides, and short
        /// enough that the last digit is never the framework's rounding showing through.
        /// </para>
        /// </remarks>
        public static string Say(float value)
        {
            // Negative zero is zero. The two print differently on the two frameworks and mean the same
            // place on the screen.
            if (value == 0f)
                return "0";

            return value.ToString("0.#####", CultureInfo.InvariantCulture);
        }
    }
}
