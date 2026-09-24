using System;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>Where an angle is chosen: a dial to turn, and what it is written as.</summary>
    /// <remarks>
    ///     Nought is straight up and positive is clockwise, as the game turns a rotor. It counts on past a
    ///     whole turn, to a whole turn either way, because the game treats a rotor sent to minus ninety and
    ///     one sent to two hundred and seventy as different instructions.
    /// </remarks>
    public sealed class Dial
    {
        /// <summary>How far the dial counts either way at most: a whole turn, as far as the game turns a rotor.</summary>
        const float Most = 360f;

        /// <summary>How far a dial drawn as a half circle turns either way.</summary>
        const float RightAngle = 90f;

        /// <summary>How much room the written angle takes: a sign, three digits, and the caret after them.</summary>
        const float WrittenWide = 5f * Sizes.Digit;

        readonly Face _face = new Face();

        readonly Bar _root = new Bar();

        float _degrees;
        float _most = Most;

        /// <summary>Makes a dial with the box its angle is written in beside it.</summary>
        public Dial()
        {
            Written.Trails = true;

            var held = new Sized(WrittenWide);
            held.Content = Written;

            // Only as wide as the dial and its box, so that a line can put it on the right.
            _root.Spacing = Space.Item;
            _root.Tight = true;
            _root.Left = _face;
            _root.Fill = held;
        }

        /// <summary>What to put in the form.</summary>
        public Element Root => _root;

        /// <summary>The box the angle is written in, which is where the question puts the caret.</summary>
        public TextBox Written { get; } = Widgets.Box(Lettering.Small, Palette.Page);

        /// <summary>Whether the dial is being turned right now.</summary>
        public bool IsDragging { get; private set; }

        /// <summary>Says how far the field lets the angle go either way, which decides the face it is drawn on.</summary>
        /// <remarks>Takes the field's bounds as written, either of which may be empty for as far as the game goes.</remarks>
        public void Reach(string least, string most)
        {
            var reach = Math.Max(Bound(least), Bound(most));
            _most = Math.Min(Most, reach);
            _face.Halved = _most <= RightAngle;
        }

        /// <summary>How far one bound lets the angle go from nought, or a whole turn where it says nothing.</summary>
        static float Bound(string written)
        {
            float bound;
            return float.TryParse(written, NumberStyles.Float, CultureInfo.InvariantCulture, out bound)
                ? Math.Abs(bound)
                : Most;
        }

        /// <summary>Takes hold of the dial where it was pressed, and turns the needle there at once.</summary>
        public bool Grab(Vector2 point)
        {
            if (!_face.Holds(point))
                return false;

            IsDragging = true;
            Carry(point);
            return true;
        }

        /// <summary>Turns the needle to follow the pointer, the short way from where it stands.</summary>
        /// <remarks>Follows how far the pointer moved round, not where it is, so going round twice counts twice.</remarks>
        public void Carry(Vector2 point)
        {
            if (!IsDragging)
                return;

            float pointing;
            if (!_face.Pointing(point, out pointing))
                return;

            var moved = Half(pointing - Wrapped(_degrees));
            Turn((float)Math.Round(_degrees + moved));
        }

        /// <summary>Lets go of the dial.</summary>
        public void Drop()
        {
            IsDragging = false;
        }

        /// <summary>Lights the dial while the pointer rests on it, and all the while it is being turned.</summary>
        public void Lit(Vector2 point)
        {
            _face.IsHot = IsDragging || _face.Holds(point);
        }

        /// <summary>Brings the needle into line with what has been typed into the box.</summary>
        /// <remarks>
        ///     Called on every tick while the question is up. Text that cannot be read is left alone, so a
        ///     half-typed angle does not fight the person typing it.
        /// </remarks>
        public void Reread()
        {
            float said;
            if (float.TryParse(Written.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out said))
            {
                _degrees = Math.Max(-_most, Math.Min(_most, said));
                _face.Degrees = _degrees;
                _face.Shows = true;
                return;
            }

            if (string.IsNullOrEmpty(Written.Text))
                _face.Shows = false;
        }

        void Turn(float degrees)
        {
            _degrees = Math.Max(-_most, Math.Min(_most, degrees));
            _face.Degrees = _degrees;
            _face.Shows = true;
            // Written with one place, as a decimal field is, though the dial turns in whole degrees.
            Written.Text = ((int)_degrees).ToString("0.0", CultureInfo.InvariantCulture);
            Written.MoveCaretEnd(false);
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

            /// <summary>Whether there is an angle to point at.</summary>
            public bool Shows;

            bool _half;

            /// <summary>Whether it is drawn as the top half of a dial, for an angle within a quarter turn.</summary>
            /// <remarks>Half as tall, so a change is a change of size.</remarks>
            public bool Halved
            {
                get { return _half; }
                set
                {
                    if (_half == value)
                        return;

                    _half = value;
                    InvalidateMeasure();
                }
            }

            /// <summary>Where the needle turns about: the middle of a whole dial, the foot of a half one.</summary>
            Vector2 Pivot
            {
                get
                {
                    var bounds = ContentBounds;
                    return Halved
                        ? new Vector2(bounds.Center.X, bounds.Y + bounds.Height - Hub)
                        : bounds.Center;
                }
            }

            /// <summary>How far from the pivot the marks reach.</summary>
            float Reach
            {
                get
                {
                    var bounds = ContentBounds;
                    return Halved
                        ? Math.Min(bounds.Width / 2f, bounds.Height - Hub)
                        : Math.Min(bounds.Width, bounds.Height) / 2f;
                }
            }

            /// <summary>Where the pointer is round the dial, clockwise from straight up, if it is far enough out to say.</summary>
            public bool Pointing(Vector2 point, out float degrees)
            {
                var centre = Pivot;
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
                return Halved ? new Vector2(Across, Across / 2f + Hub) : new Vector2(Across, Across);
            }

            protected override void DrawContent(IGraphics g)
            {
                var centre = Pivot;
                var reach = Reach;

                // A mark every fifteen degrees, longer on each quarter; a half dial has only the ones it reaches.
                var from = Halved ? -6 : 0;
                var to = Halved ? 6 : 23;
                for (var i = from; i <= to; i++)
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