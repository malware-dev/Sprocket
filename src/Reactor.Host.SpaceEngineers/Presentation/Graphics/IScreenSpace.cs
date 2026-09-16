using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Reactor.Host.SpaceEngineers.Presentation.Graphics
{
    /// <summary>
    /// The screen the game gives a mod to draw on.
    /// </summary>
    /// <remarks>
    /// Everything here is the game's own screen-space API said once, in the game's own terms: pixels,
    /// layers, materials. <see cref="ScreenSpaceGraphics" /> is what turns a layout's units into those,
    /// and <see cref="GameScreenSpace" /> is the game behind it.
    /// <para>
    /// It exists so a frame can be recorded rather than drawn. The harness answers these calls by
    /// writing them down, which is what the reference frames are, and it is the only way to compare
    /// what a screen drew with what it drew yesterday.
    /// </para>
    /// <para>
    /// <b>A scissor is pushed and popped rather than handed back as something to dispose.</b> The
    /// game's own is a struct scope, and an interface returning it as <c>IDisposable</c> would box one
    /// per clipped element per frame. So the stack belongs to whoever implements this.
    /// </para>
    /// </remarks>
    public interface IScreenSpace
    {
        /// <summary>The area the game promises is on screen, in pixels.</summary>
        RectangleF SafeArea { get; }

        /// <summary>
        /// The whole window in pixels, which is wider than the safe area on anything but 4:3.
        /// </summary>
        /// <remarks>
        /// A pointer belongs in this one: the game lets its own cursor reach the physical edges of the
        /// screen rather than the edges of its HUD.
        /// </remarks>
        Vector2 ViewportSize { get; }

        void PushScissor(Rectangle pixels, MyScreenSpaceLayer layer);

        /// <summary>Drops the innermost scissor, putting its parent's back.</summary>
        void PopScissor(MyScreenSpaceLayer layer);

        void DrawSprite(MyStringId material, Rectangle destination, Color color, float rotation,
            MyScreenSpaceLayer layer);

        void DrawText(string font, string text, Vector2 position, float scale, Color color,
            MyGuiDrawAlignEnum align, float maxWidth, MyScreenSpaceLayer layer);

        /// <summary>How big this run would be, in pixels.</summary>
        Vector2 MeasureText(string font, StringBuilder text, float scale);

        /// <summary>How tall a line of this font stands, in pixels, including leading.</summary>
        float LineHeight(string font, float scale);

        /// <summary>Where the pointer is in pixels, or false where the game is not showing one.</summary>
        bool TryCursorPixels(out Vector2 pixels);

        /// <summary>
        /// A point in the game's own normalized units, in pixels.
        /// </summary>
        /// <remarks>
        /// The game places a screen in fractions of the window and answers questions about it in
        /// pixels, so a screen host has to convert between the two. The conversion is the game's, not
        /// arithmetic worth reproducing.
        /// </remarks>
        Vector2 NormalizedToPixels(Vector2 position);

        /// <summary>A size in the game's own normalized units, in pixels.</summary>
        Vector2 NormalizedSizeToPixels(Vector2 size);
    }
}
