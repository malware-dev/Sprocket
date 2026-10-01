using Lumel.Drawing;
using Lumel.Text;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>An on/off switch: a thumb at one end, and the word for the state it is in beside it.</summary>
    /// <remarks>
    ///     The thumb is at the right when on and at the left when off, as tall as the room inside the switch
    ///     and as wide as it is tall. The body is the element's own <see cref="Element.Background" />, the room
    ///     round the thumb and the word its <see cref="Padding" />. The switch is as wide as the longer
    ///     of its two words needs, whichever it is showing, so it keeps its size when switched and a longer
    ///     word in another language widens it rather than spilling out of it.
    /// </remarks>
    public class Switch : Element, IPadded, ITextual
    {
        Thickness _padding;
        readonly Label _off = Word("OFF");
        readonly Label _on = Word("ON");

        Color _offTrack;
        Color _onTrack;
        Color _thumb;

        /// <summary>A switch that is off, in the neutral look.</summary>
        public Switch()
        {
            var children = new Collection(this);
            children.Add(_on);
            children.Add(_off);

            EdgeThickness = 1f;
            Thumb = Color.White;
        }

        /// <summary>Whether it is on.</summary>
        public bool IsOn { get; set; }

        /// <summary>Which face the words are drawn in.</summary>
        public string Font
        {
            get { return _on.Font; }
            set
            {
                _on.Font = value;
                _off.Font = value;
            }
        }

        /// <summary>How tall a line of the words is, in UI units.</summary>
        public float FontSize
        {
            get { return _on.FontSize; }
            set
            {
                _on.FontSize = value;
                _off.FontSize = value;
            }
        }

        /// <summary>The room held clear round the word.</summary>
        public Thickness WordPadding
        {
            get { return _on.Padding; }
            set
            {
                _on.Padding = value;
                _off.Padding = value;
            }
        }

        /// <summary>The color of the words.</summary>
        public Color Color
        {
            get { return _on.Color; }
            set
            {
                _on.Color = value;
                _off.Color = value;
            }
        }

        /// <summary>What the room inside the switch is filled with while on, or transparent to leave the body showing.</summary>
        public Color OnTrack
        {
            get { return _onTrack; }
            set { _onTrack = value; }
        }

        /// <summary>What the room inside the switch is filled with while off, or transparent to leave the body showing.</summary>
        public Color OffTrack
        {
            get { return _offTrack; }
            set { _offTrack = value; }
        }

        /// <summary>The color of the thumb.</summary>
        public Color Thumb
        {
            get { return _thumb; }
            set { _thumb = value; }
        }

        /// <summary>Renames the two states.</summary>
        public void Says(string on, string off)
        {
            _on.Text = on;
            _off.Text = off;
        }

        /// <summary>Room held clear between the edge and the thumb and the word.</summary>
        public Thickness Padding
        {
            get { return _padding; }
            set
            {
                if (_padding == value)
                    return;

                _padding = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _on.Measure(g, available);
            _off.Measure(g, available);

            var tall = _on.DesiredSize.Y > _off.DesiredSize.Y ? _on.DesiredSize.Y : _off.DesiredSize.Y;
            var wide = _on.DesiredSize.X > _off.DesiredSize.X ? _on.DesiredSize.X : _off.DesiredSize.X;

            // The thumb is square, so it takes as much room across as the words take down.
            return new Vector2(wide + tall, tall);
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Each word is centred here, from the width its measure already found, rather than by the label as it
        ///     draws: a label centring itself measures its words the first time it draws them, and a switch draws
        ///     one of its words for the first time on the frame it is flipped.
        /// </remarks>
        protected override void ArrangeContent(RectangleF bounds)
        {
            var thumb = bounds.Height;
            var room = bounds.Width - thumb;
            Centre(_on, bounds.X, room, bounds);
            Centre(_off, bounds.X + thumb, room, bounds);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var inside = ContentBounds;
            var track = IsOn ? _onTrack : _offTrack;
            if (track.A > 0)
                g.Fill(inside, track);

            var thumb = inside.Height;
            var left = IsOn ? inside.X + inside.Width - thumb : inside.X;
            g.Fill(new RectangleF(left, inside.Y, thumb, thumb), _thumb);

            (IsOn ? _on : _off).Draw(g);
        }

        /// <summary>Arranges <paramref name="word" /> in the middle of the room from <paramref name="left" />, <paramref name="room" /> across.</summary>
        static void Centre(Label word, float left, float room, RectangleF bounds)
        {
            var wide = word.DesiredSize.X;
            word.Arrange(new RectangleF(left + (room - wide) * 0.5f, bounds.Y, wide, bounds.Height));
        }

        static Label Word(string text)
        {
            var word = new Label();
            word.Text = text;
            word.Wraps = false;
            return word;
        }
    }
}
