using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Drawing
{
    /// <summary>
    /// The game's own screen, as the one thing that touches its screen-space API.
    /// </summary>
    /// <remarks>
    /// Every call the mod makes to <c>MyScreenSpace</c>, <c>MyScreenSpaceSprite</c> and
    /// <c>MyScreenSpaceText</c> is here and nowhere else, so what the mod needs from the game's screen
    /// can be read in one file - and so the harness can answer the same calls by writing them down.
    /// </remarks>
    public sealed class GameScreenSpace : IScreenSpace
    {
        /// <summary>
        /// The scopes the game handed back, innermost last.
        /// </summary>
        /// <remarks>
        /// A typed list rather than <c>IDisposable</c>, because <c>MyScissorScope</c> is a struct and
        /// one box per clipped element per frame is not a thing to pay for. Disposing a copy of one is
        /// safe: it reads its two fields and pops.
        /// </remarks>
        readonly List<MyScreenSpace.MyScissorScope> _scissors =
            new List<MyScreenSpace.MyScissorScope>();

        public RectangleF SafeArea
        {
            get { return MyScreenSpace.GetSafeGuiArea(); }
        }

        public Vector2 ViewportSize
        {
            get
            {
                // The camera's, which is the window rather than the HUD. Absent on a machine that has
                // not finished loading, where the safe area is the best answer available.
                if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null)
                    return MyAPIGateway.Session.Camera.ViewportSize;

                RectangleF area = MyScreenSpace.GetSafeGuiArea();
                return new Vector2(area.Width, area.Height);
            }
        }

        /// <remarks>
        /// <b>Not verified in the game yet:</b> whether a scope is recorded into a buffered layer's
        /// queue or applied when that queue replays. The harness honours it either way, and the demo
        /// mod only exercises it inside a screen's own draw.
        /// </remarks>
        public void PushScissor(Rectangle pixels, MyScreenSpaceLayer layer)
        {
            _scissors.Add(MyScreenSpace.UsingScissorRectanglePixels(pixels, layer));
        }

        public void PopScissor(MyScreenSpaceLayer layer)
        {
            int last = _scissors.Count - 1;
            if (last < 0)
                return;

            _scissors[last].Dispose();
            _scissors.RemoveAt(last);
        }

        public void DrawSprite(MyStringId material, Rectangle destination, Color color, float rotation,
            MyScreenSpaceLayer layer)
        {
            MyScreenSpaceSprite.DrawPixels(material, destination, null, color, rotation, layer);
        }

        public void DrawText(string font, string text, Vector2 position, float scale, Color color,
            MyGuiDrawAlignEnum align, float maxWidth, MyScreenSpaceLayer layer)
        {
            MyScreenSpaceText.DrawPixels(font, text, position, scale, color, align, maxWidth, layer);
        }

        public Vector2 MeasureText(string font, StringBuilder text, float scale)
        {
            return MyScreenSpaceText.MeasurePixels(font, text, scale);
        }

        public float LineHeight(string font, float scale)
        {
            // GetLineHeight answers in normalized units, and everything on this interface is pixels.
            return MyScreenSpace.NormalizedSizeToPixels(
                new Vector2(0f, MyScreenSpaceText.GetLineHeight(font, scale))).Y;
        }

        public bool TryCursorPixels(out Vector2 pixels)
        {
            return MyScreenSpace.TryGetCursorPositionPixels(out pixels);
        }

        public Vector2 NormalizedToPixels(Vector2 position)
        {
            return MyScreenSpace.NormalizedToPixels(position);
        }

        public Vector2 NormalizedSizeToPixels(Vector2 size)
        {
            return MyScreenSpace.NormalizedSizeToPixels(size);
        }
    }
}
