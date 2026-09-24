using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Drawing
{
    /// <summary>Everything the UI is allowed to draw with.</summary>
    /// <remarks>
    ///     Coordinates are UI units throughout, with the origin at the top-left of the drawable area. The
    ///     layout never rounds; rounding to pixels is the backend's.
    /// </remarks>
    public interface IGraphics
    {
        /// <summary>The drawable area, in UI units.</summary>
        Vector2 Size { get; }

        /// <summary>Restricts drawing to <paramref name="bounds" />, intersected with whatever clip is already in effect.</summary>
        /// <remarks>Every call is matched by a <see cref="PopClip" />.</remarks>
        void PushClip(RectangleF bounds);

        /// <summary>Drops the innermost clip and restores the one around it.</summary>
        void PopClip();

        /// <summary>Draws <paramref name="material" /> to fill <paramref name="destination" />, rotated about its centre.</summary>
        void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation);

        /// <summary>Draws one run of text with its top-left corner at <paramref name="position" />.</summary>
        void DrawText(string font, string text, Vector2 position, float scale, Color color);

        /// <summary>The same, with <paramref name="align" /> saying which end of the run the position names.</summary>
        void DrawText(string font, string text, Vector2 position, float scale, Color color, Align align);

        /// <summary>The size of <paramref name="text" /> if it were drawn, in UI units.</summary>
        /// <remarks>Must not allocate.</remarks>
        Vector2 MeasureText(string font, StringBuilder text, float scale);

        /// <summary>The height of one line in <paramref name="font" />, in UI units, including its leading.</summary>
        float LineHeight(string font, float scale);
    }
}