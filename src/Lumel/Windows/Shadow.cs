using Lumel.Drawing;
using VRageMath;

namespace Lumel.Windows
{
    /// <summary>What a window casts on what lies beneath it: a soft picture reaching out past its edge, lying a little away from it.</summary>
    /// <remarks>Never changes once made, so one shadow is shared by every window style that casts it.</remarks>
    public sealed class Shadow
    {
        readonly NineSlice _picture;
        readonly float _spread;
        readonly Vector2 _offset;
        readonly Color _color;

        /// <summary>A shadow drawn with <paramref name="picture" />, reaching <paramref name="spread" /> out from the window's edge, lying <paramref name="offset" /> from it, tinted <paramref name="color" />.</summary>
        public Shadow(NineSlice picture, float spread, Vector2 offset, Color color)
        {
            _picture = picture;
            _spread = spread;
            _offset = offset;
            _color = color;
        }

        /// <summary>Draws it under a window standing at <paramref name="bounds" />.</summary>
        public void Draw(IGraphics g, RectangleF bounds)
        {
            if (_spread <= 0f)
                return;

            _picture.Draw(g, new RectangleF(bounds.X + _offset.X - _spread, bounds.Y + _offset.Y - _spread,
                bounds.Width + 2f * _spread, bounds.Height + 2f * _spread), _spread, _color);
        }
    }
}
