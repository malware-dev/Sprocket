using System.Globalization;
using VRageMath;

namespace Reactor.Sim.Simulation
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

        public static string Say(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
