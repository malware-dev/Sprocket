using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Look;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Legacy
{
    /// <summary>One number between nothing and everything, set by putting a grip where it goes.</summary>
    public class Slider : Element
    {
        /// <summary>How tall a track is, matched to the boxes it stands among.</summary>
        public const float Thick = 24f;

        /// <summary>How wide the grip is.</summary>
        const float Grip = 8f;

        /// <summary>How far the grip stands out of the track, top and bottom.</summary>
        const float Lip = 3f;

        float _fraction;

        /// <summary>Makes a slider on a plain track.</summary>
        public Slider()
        {
            Track = Materials.Fill;
            TrackTint = Palette.ControlHot;
            Edge = Palette.Line;
        }

        /// <summary>The material the track is drawn with.</summary>
        public MyStringId Track { get; set; }

        /// <summary>What the track is tinted.</summary>
        /// <remarks>White for a material that is already a picture.</remarks>
        public Color TrackTint { get; set; }

        /// <summary>Where the grip is, from nothing at the left to everything at the right.</summary>
        /// <remarks>Changes only what is drawn, so it invalidates nothing.</remarks>
        public float Fraction
        {
            get { return _fraction; }
            set { _fraction = value < 0f ? 0f : value > 1f ? 1f : value; }
        }

        /// <summary>What fraction a point along the track stands for, whether or not it is on it.</summary>
        /// <remarks>
        ///     Measured over the grip's travel rather than the whole width, so that both ends of the range
        ///     can be reached.
        /// </remarks>
        public float At(Vector2 point)
        {
            var travel = ContentBounds.Width - Grip;
            if (travel <= 0f)
                return 0f;

            return (point.X - (ContentBounds.X + Grip / 2f)) / travel;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(available.X, Thick);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            g.DrawSprite(Track, ContentBounds, TrackTint, 0f);

            var left = ContentBounds.X + (ContentBounds.Width - Grip) * _fraction;
            g.Fill(
                new RectangleF(left, ContentBounds.Y - Lip, Grip, ContentBounds.Height + Lip * 2f),
                Palette.Ink);
        }
    }
}