using Lumel.Input;
using Lumel.Text;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>Something to press, on a fill of its own: its own words, or content given to it, in the middle.</summary>
    /// <remarks>
    /// The button's style dresses its own words. Content given to it is dressed by whoever gives it, and is drawn only: a press
    /// anywhere on the button reaches the button, unless <see cref="Element.InputTransparency" /> is set to let it through.
    /// </remarks>
    public class Button : ContentControl, ITextual
    {
        readonly Label _label = new Label();
        bool _pressed;

        /// <summary>A button with no words, in the neutral look.</summary>
        public Button()
        {
            Content = _label;
            InputTransparency = InputTransparency.Children;
        }

        /// <summary>The words on the button, which become what it shows in place of any content given to it; empty while it shows content given to it.</summary>
        public string Text
        {
            get { return ReferenceEquals(Content, _label) ? _label.Text : ""; }
            set
            {
                _label.Text = value;
                Content = _label;
            }
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

        /// <summary>Which face the button's own words are drawn in.</summary>
        public string Font
        {
            get { return _label.Font; }
            set { _label.Font = value; }
        }

        /// <summary>How tall a line of the button's own words is, in UI units.</summary>
        public float FontSize
        {
            get { return _label.FontSize; }
            set { _label.FontSize = value; }
        }

        /// <summary>The color the button's own words are drawn in.</summary>
        public Color Color
        {
            get { return _label.Color; }
            set { _label.Color = value; }
        }

        /// <summary>What it shows sits in the middle of the room the button is given, wider than it needs or not.</summary>
        protected override Align Aligns => Align.Center;
    }
}
