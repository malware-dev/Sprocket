using System.Collections.Generic;
using System.Text;
using Lumel.Images;
using VRage.Game.GUI.TextPanel;
using VRage.Utils;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Lumel.Drawing
{
    /// <summary>Lumel's drawing, written down as a text panel's sprites to be drawn as one frame.</summary>
    /// <remarks>
    ///     A UI unit is one of the panel's pixels, counted from the corner of the part of its texture the screen shows.
    ///     Text is measured the panel's own way, so lines break where the panel would draw them.
    /// </remarks>
    public class SpriteGraphics : IGraphics
    {
        const string Square = "SquareSimple";

        readonly IMyTextSurface _surface;
        readonly List<MySprite> _sprites;
        readonly Dictionary<string, float> _lines = new Dictionary<string, float>();
        readonly StringBuilder _letter = new StringBuilder("A");
        readonly List<RectangleF> _clips = new List<RectangleF>();
        readonly List<RenderTransform> _transforms = new List<RenderTransform>();
        readonly List<Vector2> _origins = new List<Vector2>();

        Vector2 _corner;

        /// <summary>Drawing for <paramref name="surface" />, into <paramref name="sprites" />, finding pictures in <paramref name="images" />.</summary>
        public SpriteGraphics(IMyTextSurface surface, List<MySprite> sprites, IImageCatalog images)
        {
            _surface = surface;
            _sprites = sprites;
            Images = images;
        }

        /// <inheritdoc />
        public Vector2 Size { get; private set; }

        /// <inheritdoc />
        public RectangleF ScreenBounds => new RectangleF(Vector2.Zero, Size);

        /// <inheritdoc />
        public IImageCatalog Images { get; }

        /// <inheritdoc />
        public bool DrawsParts => false;

        /// <summary>Starts a frame: the sprites written down so far are dropped, and the screen's size read again.</summary>
        public void Begin()
        {
            _sprites.Clear();
            _clips.Clear();
            _transforms.Clear();
            _origins.Clear();
            Size = _surface.SurfaceSize;
            _corner = (_surface.TextureSize - _surface.SurfaceSize) * 0.5f;
        }

        /// <summary>How tall one line of <paramref name="font" /> is at the panel's scale of one, in its pixels.</summary>
        public float LineHeight(string font)
        {
            float height;
            if (_lines.TryGetValue(font, out height))
                return height;

            height = _surface.MeasureStringInPixels(_letter, font, 1f).Y;
            if (height <= 0f)
                height = 1f;

            _lines.Add(font, height);
            return height;
        }

        /// <inheritdoc />
        public void PushClip(RectangleF bounds)
        {
            var clip = Placed(bounds);
            if (_clips.Count > 0)
            {
                var outer = _clips[_clips.Count - 1];
                RectangleF both;
                clip = RectangleF.Intersect(ref clip, ref outer, out both) ? both : new RectangleF(clip.Position, Vector2.Zero);
            }

            _clips.Add(clip);
            Clip(clip);
        }

        /// <inheritdoc />
        public void PopClip()
        {
            if (_clips.Count == 0)
                return;

            _clips.RemoveAt(_clips.Count - 1);
            if (_clips.Count > 0)
                Clip(_clips[_clips.Count - 1]);
            else
                _sprites.Add(MySprite.CreateClearClipRect());
        }

        /// <inheritdoc />
        public void PushTransform(RenderTransform transform, Vector2 origin)
        {
            _transforms.Add(transform);
            _origins.Add(origin);
        }

        /// <inheritdoc />
        public void PopTransform()
        {
            if (_transforms.Count == 0)
                return;

            _transforms.RemoveAt(_transforms.Count - 1);
            _origins.RemoveAt(_origins.Count - 1);
        }

        /// <inheritdoc />
        public void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation)
        {
            var placed = Placed(destination);
            _sprites.Add(new MySprite(SpriteType.TEXTURE, material.String, placed.Center, placed.Size, color, null, TextAlignment.CENTER, rotation));
        }

        /// <inheritdoc />
        /// <remarks>A panel draws a sprite whole, so the part is drawn as the whole picture stretched over the destination.</remarks>
        public void DrawSprite(MyStringId material, RectangleF destination, Rectangle source, Color color)
        {
            DrawSprite(material, destination, color, 0f);
        }

        /// <inheritdoc />
        public void Fill(RectangleF destination, Color color, float rotation = 0f)
        {
            var placed = Placed(destination);
            _sprites.Add(new MySprite(SpriteType.TEXTURE, Square, placed.Center, placed.Size, color, null, TextAlignment.CENTER, rotation));
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float size, Color color)
        {
            DrawText(font, text, position, size, color, TextAlign.Left);
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float size, Color color, TextAlign align)
        {
            var at = Placed(position);
            var scale = Scaled(size) / LineHeight(font);
            _sprites.Add(new MySprite(SpriteType.TEXT, text, at, null, color, font,
                align == TextAlign.Right ? TextAlignment.RIGHT : TextAlignment.LEFT, scale));
        }

        /// <inheritdoc />
        public Vector2 MeasureText(string font, StringBuilder text, float size)
        {
            var scale = size / LineHeight(font);
            return _surface.MeasureStringInPixels(text, font, scale);
        }

        void Clip(RectangleF clip)
        {
            _sprites.Add(MySprite.CreateClipRect(new Rectangle((int)clip.X, (int)clip.Y, (int)(clip.Width + 0.5f), (int)(clip.Height + 0.5f))));
        }

        /// <summary><paramref name="point" /> in the panel's texture, through every transform in force.</summary>
        Vector2 Placed(Vector2 point)
        {
            for (var i = _transforms.Count - 1; i >= 0; i--)
                point = _transforms[i].Apply(point, _origins[i]);

            return point + _corner;
        }

        RectangleF Placed(RectangleF area)
        {
            var corner = Placed(area.Position);
            return new RectangleF(corner, area.Size * Grown());
        }

        float Scaled(float size)
        {
            return size * Grown();
        }

        float Grown()
        {
            var scale = 1f;
            foreach (var transform in _transforms)
                scale *= transform.Scale;

            return scale;
        }
    }
}
