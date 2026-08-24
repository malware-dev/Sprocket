using VRage.Utils;
using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// The shapes a screen is built from that letters cannot draw.
    /// </summary>
    /// <remarks>
    /// Everything here is a <b>resource</b>, and resources are the part of drawing that both targets have
    /// to agree on. How a control composes itself out of draw calls is each renderer's own business - a
    /// screen and a text panel are two different renderers and are allowed to differ - but neither of them
    /// can invent a picture that is not there. So a material named here must resolve on both, and the mod
    /// declares each one in <c>TransparentMaterials.sbc</c> against a file the game already ships.
    /// <para>
    /// The list is short on purpose, and everything that can be drawn instead is. A pause is two
    /// rectangles, a drag grip is six small ones, a checkbox is a rectangle inside another - none of those
    /// are here, because a rectangle is crisp at any size and a texture stretched to eleven pixels is not.
    /// What is here is what rectangles cannot make: a diagonal and a curve.
    /// </para>
    /// </remarks>
    public static class Symbols
    {
        /// <summary>
        /// Flat white, tinted to whatever colour is asked for. The only way to fill a rectangle, since the
        /// seam draws sprites and knows nothing of solid colours.
        /// </summary>
        public static readonly MyStringId Fill = MyStringId.GetOrCompute("Phosphor_Fill");

        /// <summary>
        /// A single arrowhead, pointing <b>left</b> when it is not turned.
        /// </summary>
        /// <remarks>
        /// One texture for all four directions, turned by <see cref="TurnedRight" /> and the rest. A
        /// sprite can be rotated about its own centre on either target, so four files would be three
        /// files nobody has to keep in step with each other.
        /// </remarks>
        public static readonly MyStringId Chevron = MyStringId.GetOrCompute("Phosphor_Chevron");

        /// <summary>Two strokes crossing. What deleting one row out of a list is called everywhere.</summary>
        public static readonly MyStringId Cross = MyStringId.GetOrCompute("Phosphor_Cross");

        /// <summary>A filled circle.</summary>
        public static readonly MyStringId Disc = MyStringId.GetOrCompute("Phosphor_Disc");

        /// <summary>An unfilled circle, the same size and weight as <see cref="Disc" />.</summary>
        public static readonly MyStringId Ring = MyStringId.GetOrCompute("Phosphor_Ring");

        /// <summary>A quarter turn clockwise, which points <see cref="Chevron" /> up.</summary>
        public const float TurnedUp = MathHelper.PiOver2;

        /// <summary>A half turn, which points <see cref="Chevron" /> right.</summary>
        public const float TurnedRight = MathHelper.Pi;

        /// <summary>Three quarters of a turn, which points <see cref="Chevron" /> down.</summary>
        public const float TurnedDown = MathHelper.Pi + MathHelper.PiOver2;
    }
}
