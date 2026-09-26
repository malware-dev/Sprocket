using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Drawing
{
    /// <summary>The game's own screen, and the only place the mod calls its screen-space API.</summary>
    public sealed class GameScreenSpace : IScreenSpace
    {
        readonly List<MyScreenSpace.MyScissorScope> _scissors =
            new List<MyScreenSpace.MyScissorScope>();

        /// <inheritdoc />
        public RectangleF SafeArea => MyScreenSpace.GetSafeGuiArea();

        /// <summary>The camera's viewport, or the safe area's size before the session has a camera.</summary>
        public Vector2 ViewportSize
        {
            get
            {
                // The camera's viewport is the window rather than the HUD. There is no camera until the
                // session has finished loading.
                if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null)
                    return MyAPIGateway.Session.Camera.ViewportSize;

                var area = MyScreenSpace.GetSafeGuiArea();
                return new Vector2(area.Width, area.Height);
            }
        }

        /// <remarks>
        ///     Unverified in the game: whether a scope is recorded into a buffered layer's queue or applied
        ///     when that queue replays.
        /// </remarks>
        public void PushScissor(Rectangle pixels, MyScreenSpaceLayer layer)
        {
            // Kept typed, because MyScissorScope is a struct and holding it as IDisposable would box one per push.
            // Disposing a copy is safe: it reads its two fields and pops.
            _scissors.Add(MyScreenSpace.UsingScissorRectanglePixels(pixels, layer));
        }

        /// <inheritdoc />
        public void PopScissor(MyScreenSpaceLayer layer)
        {
            var last = _scissors.Count - 1;
            if (last < 0)
                return;

            _scissors[last].Dispose();
            _scissors.RemoveAt(last);
        }

        /// <inheritdoc />
        public void DrawSprite(MyStringId material, Rectangle destination, Color color, float rotation,
            MyScreenSpaceLayer layer)
        {
            MyScreenSpaceSprite.DrawPixels(material, destination, null, color, rotation, layer);
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float scale, Color color,
            MyGuiDrawAlignEnum align, float maxWidth, MyScreenSpaceLayer layer)
        {
            MyScreenSpaceText.DrawPixels(font, text, position, scale, color, align, maxWidth, layer);
        }

        /// <inheritdoc />
        public Vector2 MeasureText(string font, StringBuilder text, float scale)
        {
            return MyScreenSpaceText.MeasurePixels(font, text, scale);
        }

        /// <inheritdoc />
        public bool TryCursorPixels(out Vector2 pixels)
        {
            return MyScreenSpace.TryGetCursorPositionPixels(out pixels);
        }

        /// <inheritdoc />
        public Vector2 NormalizedToPixels(Vector2 position)
        {
            return MyScreenSpace.NormalizedToPixels(position);
        }

        /// <inheritdoc />
        public Vector2 NormalizedSizeToPixels(Vector2 size)
        {
            return MyScreenSpace.NormalizedSizeToPixels(size);
        }
    }
}