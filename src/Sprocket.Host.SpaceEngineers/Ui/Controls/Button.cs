using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>Words to press, on a fill of their own.</summary>
    /// <remarks>Pressed as one thing: a press anywhere on it, its words included, reaches the button.</remarks>
    public class Button : Element
    {
        readonly Label _label = new Label();
        bool _pressed;

        /// <summary>A button with no words, in the neutral look.</summary>
        public Button()
        {
            var children = new ElementCollection(this);
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
                Restyle();
            }
        }

        /// <summary>The button's own label, which its style writes the text half of each state onto.</summary>
        internal Label Label => _label;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _label.Measure(g, available);
            return _label.DesiredSize;
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            _label.Arrange(bounds);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _label.Draw(g);
        }
    }
}
