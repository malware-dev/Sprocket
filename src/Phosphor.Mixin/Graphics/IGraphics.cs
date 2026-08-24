using System.Text;
using VRage.Utils;
using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// Everything the UI is allowed to draw with.
    /// <para>
    /// The rule this interface is held to: <b>every member must be reproducible on an
    /// <c>IMyTextSurface</c></b>. A clip is the plain case of it - a panel has a real scissor that cuts
    /// glyphs mid-stroke, and the screen is expected to carry the same, so a container may simply clip. That is what keeps a second target possible without a rewrite, and it is
    /// why several things the screen-space API offers are absent here - a sprite sheet source rectangle, an
    /// arbitrary rotation pivot, a draw layer, a text width limit. <c>MySprite</c> has none of them, so
    /// neither does this. A backend may still use them internally; it may not expose them.
    /// </para>
    /// <para>
    /// What the rule does <b>not</b> reach is how a backend turns a call into pixels, or how a control
    /// composes itself out of calls. Those are each renderer's own business, and the two renderers are
    /// genuinely different machines. Three consequences worth writing down, because each was found the
    /// hard way:
    /// <list type="bullet">
    /// <item><description>
    /// <b>Rounding belongs in the backend, never in the layout.</b> <c>MyScreenSpaceSprite.DrawPixels</c>
    /// takes a <c>Rectangle</c>, so the screen has to round and the only question is how; <c>MySprite</c>
    /// takes <c>Vector2?</c>, so a surface never has to and should not. If <see cref="Element.Arrange" />
    /// ever rounds, that choice is taken away from both at once. It is a rasterising concern and it stays
    /// below this line.
    /// </description></item>
    /// <item><description>
    /// <b>Round position and size apart, not the edges outward.</b> Whatever a backend does, the property
    /// worth having is that the same size is the same number of pixels wherever it sits. Rounding the near
    /// edge down and the far edge up does not have it: the same control in two rows of different heights
    /// came out thirty pixels in one and twenty-nine in the other, and a one-unit border came out two
    /// pixels on the sides whose span crossed a boundary.
    /// </description></item>
    /// <item><description>
    /// <b>The targets are not the same size and it is not close.</b> A screen is 1440x1080 units. A text
    /// panel's render target is 512, 256 or 128 at whatever aspect its block declares - so a unit is
    /// roughly a quarter of a texel there, and a tenth of one on the smallest. Anything a unit thick is
    /// sub-texel, which is why rounding on that target would erase every hairline on the screen rather
    /// than tidy it. It is also why this rule promises that <i>a</i> Phosphor UI can be drawn on a
    /// surface, and not that <i>any given</i> one can.
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Coordinates are <b>UI units</b> throughout, with the origin at the top-left of the drawable area. A
    /// unit is a pixel on a target that does not scale, and a target that does scale hides that behind
    /// <see cref="Size" />: a text panel is its own unchanging pixel size, and the screen is a fixed size the
    /// game stretches to fit the monitor. So a layout is written once and is not secretly correct only at
    /// the resolution it was written on.
    /// </para>
    /// </summary>
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

        /// <summary>
        /// Draws one run of text with its top-left corner at <paramref name="position" />.
        /// <para>
        /// There is no alignment argument. A text panel aligns horizontally and not vertically, so an
        /// aligned API here could not be reproduced; and a layout that measures can place text itself, which
        /// it has to do anyway. Alignment is the framework's job, above this line.
        /// </para>
        /// </summary>
        void DrawText(string font, string text, Vector2 position, float scale, Color color);

        /// <summary>The size of <paramref name="text" /> if it were drawn, in UI units. Allocates nothing.</summary>
        Vector2 MeasureText(string font, StringBuilder text, float scale);

        /// <summary>The height of one line in <paramref name="font" />, in UI units, including its leading.</summary>
        float LineHeight(string font, float scale);
    }
}
