using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Drawing
{
    /// <summary>Everything the UI is allowed to draw with.</summary>
    /// <remarks>
    /// <b>Every member must be reproducible on an <c>IMyTextSurface</c>.</b> That is what keeps a second
    /// target possible, and it is why several things the screen-space API offers are absent here: a
    /// sprite sheet source rectangle, an arbitrary rotation pivot, a draw layer, a text width limit.
    /// <c>MySprite</c> has none of them, so neither does this. A backend may use them internally; it may
    /// not expose them.
    /// <para>
    /// <b>Rounding belongs in the backend, never in the layout.</b>
    /// <c>MyScreenSpaceSprite.DrawPixels</c> takes a <c>Rectangle</c>, so the screen has to round;
    /// <c>MySprite</c> takes <c>Vector2?</c>, so a surface never has to. If <c>Element.Arrange</c>
    /// rounds, that choice is taken away from both at once.
    /// </para>
    /// <para>
    /// <b>Round position and size apart, not the edges outward</b>, so that the same size is the same
    /// number of pixels wherever it sits. Rounding the near edge down and the far edge up does not have
    /// that property: a span crossing a boundary comes out a pixel wider than the same span that does
    /// not.
    /// </para>
    /// <para>
    /// <b>The targets are not the same size and it is not close.</b> A screen is 1440x1080 units; a text
    /// panel's render target is 512, 256 or 128 at whatever aspect its block declares, so a unit is
    /// roughly a quarter of a texel there and a tenth of one on the smallest. Anything a unit thick is
    /// sub-texel, so rounding on that target would erase every hairline rather than tidy it. This rule
    /// promises that <i>a</i> UI built on this can be drawn on a surface, not that <i>any given</i> one can.
    /// </para>
    /// <para>
    /// Coordinates are <b>UI units</b> throughout, with the origin at the top-left of the drawable area.
    /// A unit is a pixel on a target that does not scale, and a target that does scale hides that behind
    /// <see cref="Size" />.
    /// </para>
    /// </remarks>
    public interface IGraphics
    {
        /// <summary>The drawable area, in UI units. A surface's size, or the fixed size the screen scales.</summary>
        Vector2 Size { get; }

        /// <summary>
        /// Restricts drawing to <paramref name="bounds" />, intersected with whatever clip is already in
        /// effect. Match every call with a <see cref="PopClip" />.
        /// </summary>
        void PushClip(RectangleF bounds);

        /// <summary>Drops the innermost clip and restores the one around it.</summary>
        void PopClip();

        /// <summary>
        /// Draws <paramref name="material" /> to fill <paramref name="destination" />, rotated about the
        /// destination's own centre. Centre only, because that is all a <c>MySprite</c> can do.
        /// </summary>
        void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation);

        /// <summary>Draws one run of text with its top-left corner at <paramref name="position" />.</summary>
        void DrawText(string font, string text, Vector2 position, float scale, Color color);

        /// <summary>
        /// The same, with <paramref name="align" /> saying which end of the run the position names.
        /// </summary>
        /// <remarks>
        /// Across only. A text panel aligns horizontally and not vertically, so an alignment naming both
        /// could not be reproduced on both targets, and vertical alignment stays the framework's job.
        /// <para>
        /// It is here rather than left to callers because the alternative is measuring: a run set against
        /// a right edge has to be measured first and subtracted, by every caller that wants one, on every
        /// frame the position moves. Both targets can place it for nothing.
        /// </para>
        /// </remarks>
        void DrawText(string font, string text, Vector2 position, float scale, Color color, Align align);

        /// <summary>The size of <paramref name="text" /> if it were drawn, in UI units. Allocates nothing.</summary>
        Vector2 MeasureText(string font, StringBuilder text, float scale);

        /// <summary>The height of one line in <paramref name="font" />, in UI units, including its leading.</summary>
        float LineHeight(string font, float scale);
    }
}
