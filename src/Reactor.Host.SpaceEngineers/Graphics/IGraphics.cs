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
