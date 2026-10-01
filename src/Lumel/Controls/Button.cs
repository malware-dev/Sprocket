using System;
using Lumel.Drawing;
using Lumel.Text;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>Words to press, on a fill of their own.</summary>
    /// <remarks>Pressed as one thing: a press anywhere on it, its words included, reaches the button.</remarks>
    public class Button : Element, IPadded, ITextual
    {
        Thickness _padding;
        readonly Label _label = new Label();
        bool _pressed;

        /// <summary>A button with no words, in the neutral look.</summary>
        public Button()
        {
            var children = new Collection(this);
            children.Add(_label);
        }

        /// <summary>The words on the button.</summary>
        public string Text
        {
            get { return _label.Text; }
            set { _label.Text = value; }
        }

        /// <summary>Whether the pointer is pressed down on the button.</summary>
        public bool IsPressed
        {
            get { return _pressed; }
            set
            {
                if (_pressed == value)
                    return;

                _pressed = value;
                StateChanged();
            }
        }

        /// <summary>Which face the words are drawn in.</summary>
        public string Font
        {
            get { return _label.Font; }
            set { _label.Font = value; }
        }

        /// <summary>How tall a line of the words is, in UI units.</summary>
        public float FontSize
        {
            get { return _label.FontSize; }
            set { _label.FontSize = value; }
        }

        /// <summary>The color the words are drawn in.</summary>
        public Color Color
        {
            get { return _label.Color; }
            set { _label.Color = value; }
        }

        /// <summary>Room held clear between the edge and the words.</summary>
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
            _label.Measure(g, available);
            return _label.DesiredSize;
        }

        /// <summary>Puts the words in the middle of the room the button is given, wider than they need or not.</summary>
        protected override void ArrangeContent(RectangleF bounds)
        {
            var width = Math.Min(_label.DesiredSize.X, bounds.Width);
            _label.Arrange(new RectangleF(bounds.X + (bounds.Width - width) * 0.5f, bounds.Y, width, bounds.Height));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _label.Draw(g);
        }
    }
}
