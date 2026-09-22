using System;
using System.Globalization;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Drawing;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Look;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// Where an angle is chosen: a dial to turn, and what it is written as.
    /// </summary>
    /// <remarks>
    /// Nought is straight up and the needle turns clockwise, the way the game turns a rotor clockwise.
    /// Turning it past a whole turn carries on counting, to a whole turn either way, because a rotor
    /// told to go round to minus ninety is not the same instruction as one told to go to two hundred
    /// and seventy. The dial turns in whole degrees; the box takes whatever is typed into it.
    /// <para>
    /// The box is this control's own, and the question hands the caret to it in place of the field's,
    /// as it does for a colour.
    /// </para>
    /// </remarks>
    public sealed class Dial
    {
        /// <summary>How far the dial counts either way: a whole turn, which is what the game will turn to.</summary>
        const float Most = 360f;

        /// <summary>How much room the written angle takes: a sign, three digits, and the caret after them.</summary>
        const float WrittenWide = 5f * Sizes.Digit;

        readonly Bar _root = new Bar();
        readonly Face _face = new Face();
        readonly TextBox _written = Widgets.Box(Lettering.Body, Palette.Page);


        float _degrees;
        bool _held;

        public Dial()
        {
            _written.Trails = true;

            var held = new Sized(WrittenWide);
            held.Content = _written;

            _root.Spacing = Space.Item;
            _root.Left = _face;
            _root.Fill = held;
        }

        /// <summary>What to put in the form.</summary>
        public Element Root
        {
            get { return _root; }
        }

        /// <summary>The box the angle is written in, which is where the question puts the caret.</summary>
        public TextBox Written
        {
            get { return _written; }
        }

        /// <summary>Whether the dial is being turned right now.</summary>
        public bool IsDragging
        {
            get { return _held; }
        }

        /// <summary>Takes hold of the dial where it was pressed, and turns the needle there at once.</summary>
        public bool Grab(Vector2 point)
        {
            if (!_face.Holds(point))
                return false;

            _held = true;
            Carry(point);
            return true;
        }

        /// <summary>
        /// Turns the needle to follow the pointer, the short way from where it stands.
        /// </summary>
        /// <remarks>
        /// By how far the pointer moved round rather than by where it is, so going round twice
        /// counts twice rather than landing back where it began.
        /// </remarks>
        public void Carry(Vector2 point)
        {
            if (!_held)
                return;

            float pointing;
            if (!_face.Pointing(point, out pointing))
                return;

            var moved = Half(pointing - Wrapped(_degrees));
            Turn((float)Math.Round(_degrees + moved));
        }

        public void Drop()
        {
            _held = false;
        }

        /// <summary>Lights the dial while the pointer rests on it, and all the while it is being turned.</summary>
        public void Lit(Vector2 point)
        {
            _face.IsHot = _held || _face.Holds(point);
        }

        /// <summary>
        /// Brings the needle into line with what has been typed into the box.
        /// </summary>
        /// <remarks>
        /// Read every tick while the question is up, because the text is the one part somebody can
        /// change without this control hearing about it. What cannot be read is left alone rather than
        /// corrected, so a half-typed angle does not fight the person typing it.
        /// </remarks>
        public void Reread()
        {
            float said;
            if (float.TryParse(_written.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out said))
            {
                _degrees = Math.Max(-Most, Math.Min(Most, said));
                _face.Degrees = _degrees;
                _face.Shows = true;
                return;
            }

            if (string.IsNullOrEmpty(_written.Text))
                _face.Shows = false;
        }

        void Turn(float degrees)
        {
            _degrees = Math.Max(-Most, Math.Min(Most, degrees));
            _face.Degrees = _degrees;
            _face.Shows = true;
            _written.Text = ((int)_degrees).ToString(CultureInfo.InvariantCulture);
            _written.MoveCaretEnd(false);
        }

        /// <summary>An angle brought into one turn, nought to three hundred and sixty.</summary>
        static float Wrapped(float degrees)
        {
            var wrapped = degrees % 360f;
            return wrapped < 0 ? wrapped + 360f : wrapped;
        }

        /// <summary>A difference of angles brought within half a turn either way.</summary>
        static float Half(float degrees)
        {
            var half = Wrapped(degrees + 180f) - 180f;
            return half;
        }

        /// <summary>The dial itself: marks round the edge, and a needle from the middle.</summary>
        sealed class Face : Element
        {
            /// <summary>How big the dial is drawn.</summary>
            const float Across = 96f;

            const float Mark = 6f;
            const float Quarter = 12f;
            const float Thin = 2f;
            const float Needle = 3f;
            const float Hub = 6f;

            /// <summary>Where the needle points, clockwise from straight up.</summary>
            public float Degrees;

            /// <summary>Whether there is an angle to point at, as against a field nobody has filled.</summary>
            public bool Shows;

            /// <summary>Where the pointer is round the dial, clockwise from straight up, if it is far enough out to say.</summary>
            public bool Pointing(Vector2 point, out float degrees)
            {
                var centre = ContentBounds.Center;
                var dx = point.X - centre.X;
                var dy = point.Y - centre.Y;

                degrees = 0f;
                if (dx * dx + dy * dy < Hub * Hub)
                    return false;

                degrees = (float)(Math.Atan2(dx, -dy) * 180.0 / Math.PI);
                return true;
            }

            protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
            {
                return new Vector2(Across, Across);
            }

            protected override void DrawContent(IGraphics g)
            {
                var centre = ContentBounds.Center;
                var reach = Math.Min(ContentBounds.Width, ContentBounds.Height) / 2f;

                // A mark every fifteen degrees and a longer one on each quarter, so a right angle can be
                // found by eye and the needle read to within a mark.
                for (var i = 0; i < 24; i++)
                {
                    var quarter = i % 6 == 0;
                    var length = quarter ? Quarter : Mark;
                    Line(g, centre, i * 15f, reach - length, reach, quarter ? Palette.Muted : Palette.Faint, Thin);
                }

                var hand = IsHot ? Palette.Live : Palette.Ink;
                if (Shows)
                    Line(g, centre, Degrees, 0f, reach - Quarter - Thin, hand, Needle);

                g.DrawSprite(Solid, new RectangleF(centre.X - Hub / 2f, centre.Y - Hub / 2f, Hub, Hub), hand, 0f);
            }

            /// <summary>A stroke out from the middle at an angle, between two distances from it.</summary>
            static void Line(IGraphics g, Vector2 centre, float degrees, float from, float to, Color colour, float thick)
            {
                var turned = MathHelper.ToRadians(degrees);
                var along = new Vector2((float)Math.Sin(turned), -(float)Math.Cos(turned));
                var middle = centre + along * ((from + to) / 2f);
                var length = to - from;

                g.DrawSprite(Solid, new RectangleF(middle.X - thick / 2f, middle.Y - length / 2f, thick, length),
                    colour, turned);
            }
        }
    }
}
