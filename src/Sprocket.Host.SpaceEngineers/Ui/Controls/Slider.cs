using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Look;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>One number between nothing and everything, set by putting a grip where it goes.</summary>
    /// <remarks>
    ///     The track may be any material, which is the whole reason this is not a scrollbar: a hue runs
    ///     along its own track and the track is what says so. Saturation and value have nothing to show and
    ///     take a plain one, which is how the game draws the same three.
    /// </remarks>
    public class Slider : Element
    {
        /// <summary>How tall a track is, matched to the boxes it stands among rather than chosen.</summary>
        public const float Thick = 24f;

        /// <summary>How wide the grip is, and how far the track is inset so it stays on it.</summary>
        const float Grip = 8f;

        /// <summary>How far the grip stands out of the track, top and bottom, so it can be seen on a dark one.</summary>
        const float Lip = 3f;

        float _fraction;

        public Slider()
        {
            Track = Solid;
            TrackTint = Palette.ControlHot;
            Edge = Palette.Line;
        }

        /// <summary>The material the track is drawn with.</summary>
        public MyStringId Track { get; set; }

        /// <summary>What the track is tinted, which is white where the material is already a picture.</summary>
        public Color TrackTint { get; set; }

        /// <summary>
        ///     Where the grip is, from nothing at the left to everything at the right.
        /// </summary>
        /// <remarks>
        ///     Neither measure nor arrangement, so neither is invalidated: moving a grip changes what is
        ///     drawn inside a box whose size and place are already settled. The same reason a colour change
        ///     invalidates nothing.
        /// </remarks>
        public float Fraction
        {
            get { return _fraction; }
            set { _fraction = value < 0f ? 0f : value > 1f ? 1f : value; }
        }

        /// <summary>What fraction a point along the track stands for, whether or not it is on it.</summary>
        /// <remarks>
        ///     Worked out from the travel the grip actually has rather than from the whole width, so that
        ///     pressing the far left gives nothing and the far right gives everything. Measuring against the
        ///     whole width leaves half a grip at each end that cannot be reached.
        /// </remarks>
        public float At(Vector2 point)
        {
            var travel = ContentBounds.Width - Grip;
            if (travel <= 0f)
                return 0f;

            return (point.X - (ContentBounds.X + Grip / 2f)) / travel;
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(available.X, Thick);
        }

        protected override void DrawContent(IGraphics g)
        {
            g.DrawSprite(Track, ContentBounds, TrackTint, 0f);

            var left = ContentBounds.X + (ContentBounds.Width - Grip) * _fraction;
            g.DrawSprite(Solid,
                new RectangleF(left, ContentBounds.Y - Lip, Grip, ContentBounds.Height + Lip * 2f),
                Palette.Ink, 0f);
        }
    }
}