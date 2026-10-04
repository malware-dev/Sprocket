using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>The screen the game gives a mod to draw on, in the game's own terms.</summary>
    /// <remarks>
    ///     The harness implements this to record a frame instead of drawing it. The game's scissor is a
    ///     struct scope, so each implementation keeps its own stack of them.
    /// </remarks>
    public interface IScreenSpace
    {
        /// <summary>The area the game promises is on screen, in pixels.</summary>
        RectangleF SafeArea { get; }

        /// <summary>The whole window in pixels, which is wider than the safe area on anything but 4:3.</summary>
        /// <remarks>The game's own cursor reaches the edges of this, not of the safe area.</remarks>
        Vector2 ViewportSize { get; }

        /// <summary>
        ///     Cuts what is drawn on <paramref name="layer" /> to <paramref name="pixels" /> until the matching pop.
        /// </summary>
        void PushScissor(Rectangle pixels, MyScreenSpaceLayer layer);

        /// <summary>Drops the innermost scissor, putting its parent's back.</summary>
        void PopScissor(MyScreenSpaceLayer layer);

        /// <summary>
        ///     Draws <paramref name="material" /> stretched over <paramref name="destination" />, tinted and rotated.
        /// </summary>
        void DrawSprite(MyStringId material, Rectangle destination, Color color, float rotation,
            MyScreenSpaceLayer layer);

        /// <summary>How big <paramref name="material" />'s picture was made, in its own pixels, or zero for a material this cannot draw.</summary>
        Vector2 TextureSize(MyStringId material);

        /// <summary>
        ///     Draws one run of text at <paramref name="position" />, placed about it as <paramref name="align" /> says.
        /// </summary>
        void DrawText(string font, string text, Vector2 position, float scale, Color color,
            MyGuiDrawAlignEnum align, float maxWidth, MyScreenSpaceLayer layer);

        /// <summary>How big this run would be, in pixels.</summary>
        /// <remarks>An empty run measures as one line, as tall as the game draws a line.</remarks>
        Vector2 MeasureText(string font, StringBuilder text, float scale);

        /// <summary>How tall one line of <paramref name="font" /> is in the font's own units, as its file says, or zero where that cannot be read.</summary>
        int LineUnits(string font);

        /// <summary>Where the pointer is in pixels, or false where the game is not showing one.</summary>
        bool TryCursorPixels(out Vector2 pixels);

        /// <summary>A point in the game's own normalized units, in pixels.</summary>
        Vector2 NormalizedToPixels(Vector2 position);

        /// <summary>A size in the game's own normalized units, in pixels.</summary>
        Vector2 NormalizedSizeToPixels(Vector2 size);
    }
}